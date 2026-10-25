using FinanceManager.Application.CreditCards.Requests;
using FinanceManager.Application.CreditCards.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.CreditCards;

public sealed class CreditCardsService(
    ICreditCardRepository repository,
    IFinancialAccountRepository financialAccountRepository)
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
            request.SettlementAccountId);

        await repository.AddAsync(creditCard, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(creditCard);
    }

    public async Task<IReadOnlyList<CreditCardResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        // No manual household filtering here: the global EF Core query filter on
        // IHouseholdScoped entities already restricts `repository.GetAllAsync` to the
        // current request's household - every member of the household can see every card in
        // it, only mutation (Update/Delete) is further restricted to owners.
        var creditCards = await repository.GetAllAsync(cancellationToken);

        return creditCards
            .Where(x => x.IsActive)
            .OrderBy(x => x.CreatedAt)
            .Select(Map)
            .ToList();
    }

    public async Task<CreditCardResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var creditCard = await GetActiveOrThrowAsync(id, cancellationToken);

        return Map(creditCard);
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
            request.SettlementAccountId);

        await repository.SaveChangesAsync(cancellationToken);

        return Map(creditCard);
    }

    public async Task DeleteAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken)
    {
        var creditCard = await GetOwnedOrThrowAsync(id, requestingUserId, cancellationToken);

        creditCard.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<CreditCard> GetActiveOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var creditCard = await repository.GetByIdAsync(id, cancellationToken);

        return creditCard is { IsActive: true } ? creditCard : throw NotFoundException.For(nameof(CreditCard), id);
    }

    /// <summary>
    /// Same existence check as <see cref="GetActiveOrThrowAsync"/>, plus an ownership check -
    /// a household member who is not an owner of the card gets the exact same
    /// <see cref="NotFoundException"/> as a cross-household caller would (no 403 precedent
    /// anywhere in this codebase - see FinancialAccountsService for the identical pattern).
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

    private static CreditCardResponse Map(CreditCard creditCard) =>
        new(
            creditCard.Id,
            creditCard.Brand,
            creditCard.CreditLimit,
            creditCard.StatementClosingDay,
            creditCard.PaymentDueDay,
            creditCard.SettlementAccountId,
            creditCard.OwnerUserIds,
            creditCard.IsActive,
            creditCard.CreatedAt,
            creditCard.UpdatedAt);
}
