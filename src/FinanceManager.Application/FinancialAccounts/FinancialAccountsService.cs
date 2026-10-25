using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.FinancialAccounts;

public sealed class FinancialAccountsService(IFinancialAccountRepository repository)
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

        return Map(account);
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

        return accounts
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(Map)
            .ToList();
    }

    public async Task<FinancialAccountResponse> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await GetActiveOrThrowAsync(id, cancellationToken);

        return Map(account);
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

        return Map(account);
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
    /// Same existence check as <see cref="GetActiveOrThrowAsync"/>, plus an ownership check -
    /// a household member who is not an owner of the account gets the exact same
    /// <see cref="NotFoundException"/> as a cross-household caller would, rather than a
    /// distinguishable 403: the codebase has no precedent anywhere for mapping a domain
    /// exception to 403 (see <c>DomainExceptionHandler</c>), and leaking "this exists but
    /// isn't yours" would contradict the project's existing "don't leak existence" pattern.
    /// </summary>
    private async Task<FinancialAccount> GetOwnedOrThrowAsync(
        Guid id,
        Guid requestingUserId,
        CancellationToken cancellationToken)
    {
        var account = await GetActiveOrThrowAsync(id, cancellationToken);

        return account.IsOwnedBy(requestingUserId) ? account : throw NotFoundException.For(nameof(FinancialAccount), id);
    }

    private static FinancialAccountResponse Map(FinancialAccount account) =>
        new(
            account.Id,
            account.Name,
            account.Type,
            account.InstitutionName,
            account.OwnerUserIds,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt);
}
