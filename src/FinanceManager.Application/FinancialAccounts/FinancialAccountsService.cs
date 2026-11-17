using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.FinancialAccounts;

public sealed class FinancialAccountsService(
    IFinancialAccountRepository repository,
    ITransactionRepository transactionRepository)
{
    public async Task<FinancialAccountResponse> CreateAsync(
        CreateFinancialAccountRequest request,
        Guid householdId,
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var account = new FinancialAccount(
            request.Name,
            request.Type,
            householdId,
            ownerUserId,
            request.InstitutionName);

        await repository.AddAsync(account, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return await MapAsync(account, cancellationToken);
    }

    public async Task<IReadOnlyList<FinancialAccountResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        // No manual household filtering here: the global EF Core query filter on
        // IHouseholdScoped entities (see FinanceManagerDbContext) already restricts
        // `repository.GetAllAsync` to the current request's household. Every member of the
        // household can see every account in it, regardless of who owns it - only
        // mutation (Update/Delete) is further restricted to owners (see GetOwnedOrThrowAsync).
        var accounts = await repository.GetAllAsync(cancellationToken);

        var active = accounts
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ToList();

        var responses = new List<FinancialAccountResponse>(active.Count);
        foreach (var account in active)
        {
            responses.Add(await MapAsync(account, cancellationToken));
        }

        return responses;
    }

    public async Task<FinancialAccountResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await GetActiveOrThrowAsync(id, cancellationToken);

        return await MapAsync(account, cancellationToken);
    }

    public async Task<FinancialAccountResponse> UpdateAsync(
        Guid id,
        UpdateFinancialAccountRequest request,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var account = await GetOwnedOrThrowAsync(id, requestingUserId, cancellationToken);

        account.Update(request.Name, request.Type, request.InstitutionName);
        await repository.SaveChangesAsync(cancellationToken);

        return await MapAsync(account, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, Guid requestingUserId, CancellationToken cancellationToken)
    {
        var account = await GetOwnedOrThrowAsync(id, requestingUserId, cancellationToken);

        account.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<FinancialAccount> GetActiveOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(id, cancellationToken);

        return account is { IsActive: true } ? account : throw NotFoundException.For(nameof(FinancialAccount), id);
    }

    /// <summary>
    /// Same as <see cref="GetActiveOrThrowAsync"/>, but also requires the caller to be an owner -
    /// non-owners get <see cref="NotFoundException"/> rather than a 403, so existence is never leaked.
    /// </summary>
    private async Task<FinancialAccount> GetOwnedOrThrowAsync(
        Guid id,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var account = await GetActiveOrThrowAsync(id, cancellationToken);

        return account.IsOwnedBy(requestingUserId) ? account : throw NotFoundException.For(nameof(FinancialAccount), id);
    }

    private async Task<FinancialAccountResponse> MapAsync(FinancialAccount account, CancellationToken cancellationToken)
    {
        var balance = await transactionRepository.GetAccountBalanceAsync(account.Id, cancellationToken);

        return new FinancialAccountResponse(
            account.Id,
            account.Name,
            account.Type,
            account.InstitutionName,
            account.OwnerUserIds,
            balance,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt);
    }
}
