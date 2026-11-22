using FinanceManager.Application.CreditCards.Requests;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Application.Transactions;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.CreditCards;

public sealed class CreditCardsService(
    ICreditCardRepository repository,
    IFinancialAccountRepository financialAccountRepository,
    ITransactionRepository transactionRepository)
{
    public async Task<CreditCardResponse> CreateAsync(
        CreateCreditCardRequest request,
        Guid householdId,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        await ValidateSettlementAccountAsync(request.SettlementAccountId, cancellationToken);

        var creditCard = new CreditCard(
            householdId,
            ownerUserId,
            request.CreditLimit,
            request.StatementClosingDay,
            request.PaymentDueDay,
            request.Brand,
            request.SettlementAccountId,
            request.Color);

        await repository.AddAsync(creditCard, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return await MapAsync(creditCard, cancellationToken);
    }

    public async Task<IReadOnlyList<CreditCardResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        // No manual household filtering here: the global EF Core query filter on
        // IHouseholdScoped entities already restricts `repository.GetAllAsync` to the
        // current request's household - every member of the household can see every card in
        // it, only mutation (Update/Delete) is further restricted to owners.
        var creditCards = await repository.GetAllAsync(cancellationToken);

        var active = creditCards
            .Where(x => x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .ToList();

        var responses = new List<CreditCardResponse>(active.Count);
        foreach (var creditCard in active)
        {
            responses.Add(await MapAsync(creditCard, cancellationToken));
        }

        return responses;
    }

    public async Task<CreditCardResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var creditCard = await GetActiveOrThrowAsync(id, cancellationToken);

        return await MapAsync(creditCard, cancellationToken);
    }

    public async Task<CreditCardResponse> UpdateAsync(
        Guid id,
        UpdateCreditCardRequest request,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var creditCard = await GetOwnedOrThrowAsync(id, requestingUserId, cancellationToken);

        await ValidateSettlementAccountAsync(request.SettlementAccountId, cancellationToken);

        creditCard.Update(
            request.CreditLimit,
            request.StatementClosingDay,
            request.PaymentDueDay,
            request.Brand,
            request.SettlementAccountId,
            request.Color);

        await repository.SaveChangesAsync(cancellationToken);

        return await MapAsync(creditCard, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken)
    {
        var creditCard = await GetOwnedOrThrowAsync(id, requestingUserId, cancellationToken);

        creditCard.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Creates the two settlement legs (account Expense + card Income) atomically: both are
    /// added to the same <see cref="ITransactionRepository"/>-backed DbContext and persisted in a
    /// single <c>SaveChangesAsync</c> call, so either both are written or neither is.
    /// </summary>
    public async Task<CreditCardPaymentResponse> CreatePaymentAsync(
        Guid creditCardId,
        CreateCreditCardPaymentRequest request,
        Guid householdId,
        Guid recordedByUserId,
        CancellationToken cancellationToken)
    {
        var creditCard = await GetActiveOrThrowAsync(creditCardId, cancellationToken);
        var fromAccount = await financialAccountRepository.GetByIdAsync(request.FromFinancialAccountId, cancellationToken);

        if (fromAccount is not { IsActive: true })
        {
            throw NotFoundException.For(nameof(FinancialAccount), request.FromFinancialAccountId);
        }

        var currency = request.Currency ?? Transaction.SupportedCurrency;
        var settlementGroupId = Guid.NewGuid();

        var accountLeg = Transaction.CreateAccountSettlementLeg(
            fromAccount.Id,
            recordedByUserId,
            request.Amount,
            currency,
            request.Date,
            householdId,
            settlementGroupId,
            request.Description);

        var creditCardLeg = Transaction.CreateCreditCardSettlementLeg(
            creditCard.Id,
            recordedByUserId,
            request.Amount,
            currency,
            request.Date,
            householdId,
            settlementGroupId,
            request.Description);

        await transactionRepository.AddAsync(accountLeg, cancellationToken);
        await transactionRepository.AddAsync(creditCardLeg, cancellationToken);
        await transactionRepository.SaveChangesAsync(cancellationToken);

        return new CreditCardPaymentResponse(
            settlementGroupId,
            TransactionMapper.Map(accountLeg),
            TransactionMapper.Map(creditCardLeg));
    }

    /// <summary>
    /// Archives both legs of the same <paramref name="settlementGroupId"/> together - a
    /// payment is never left half-undone.
    /// </summary>
    public async Task ArchivePaymentAsync(
        Guid creditCardId,
        Guid settlementGroupId,
        CancellationToken cancellationToken)
    {
        await GetActiveOrThrowAsync(creditCardId, cancellationToken);

        var legs = await transactionRepository.GetBySettlementGroupIdAsync(settlementGroupId, cancellationToken);
        var activeLegs = legs.Where(x => x.IsActive).ToList();

        if (activeLegs.Count == 0 || activeLegs.All(x => x.CreditCardId != creditCardId))
        {
            throw NotFoundException.For("CreditCardPayment", settlementGroupId);
        }

        foreach (var leg in activeLegs)
        {
            leg.Deactivate();
        }

        await transactionRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task<CreditCard> GetActiveOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var creditCard = await repository.GetByIdAsync(id, cancellationToken);

        return creditCard is { IsActive: true } ? creditCard : throw NotFoundException.For(nameof(CreditCard), id);
    }

    /// <summary>
    /// Same as <see cref="GetActiveOrThrowAsync"/>, but also requires the caller to be an owner -
    /// non-owners get <see cref="NotFoundException"/> rather than a 403, so existence is never leaked.
    /// </summary>
    private async Task<CreditCard> GetOwnedOrThrowAsync(
        Guid id,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var creditCard = await GetActiveOrThrowAsync(id, cancellationToken);

        return creditCard.IsOwnedBy(requestingUserId) ? creditCard : throw NotFoundException.For(nameof(CreditCard), id);
    }

    private async Task ValidateSettlementAccountAsync(
        Guid? settlementAccountId,
        CancellationToken cancellationToken)
    {
        if (settlementAccountId is null)
        {
            return;
        }

        // Looking the settlement account up through the household-scoped repository means
        // cross-household references are rejected automatically (the global query filter
        // already restricts GetByIdAsync to the caller's household) - a card cannot settle
        // against another household's account.
        var settlementAccount = await financialAccountRepository.GetByIdAsync(settlementAccountId.Value, cancellationToken);

        if (settlementAccount is null || !settlementAccount.IsActive)
        {
            throw new BusinessRuleViolationException("SettlementAccountId does not reference an existing account.");
        }
    }

    private async Task<CreditCardResponse> MapAsync(CreditCard creditCard, CancellationToken cancellationToken)
    {
        var currentDebt = await transactionRepository.GetCreditCardDebtAsync(creditCard.Id, cancellationToken);

        return new CreditCardResponse(
            creditCard.Id,
            creditCard.Brand,
            creditCard.CreditLimit,
            creditCard.StatementClosingDay,
            creditCard.PaymentDueDay,
            creditCard.SettlementAccountId,
            creditCard.Color,
            creditCard.OwnerUserIds,
            currentDebt,
            creditCard.IsActive,
            creditCard.CreatedAt,
            creditCard.UpdatedAt);
    }
}
