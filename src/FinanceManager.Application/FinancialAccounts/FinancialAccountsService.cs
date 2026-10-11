using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.FinancialAccounts;

public sealed class FinancialAccountsService(IFinancialAccountRepository repository)
{
    public async Task<FinancialAccountResponse> CreateAsync(
        CreateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = new FinancialAccount(request.Name, request.Type, request.InstitutionName);

        await repository.AddAsync(account, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(account);
    }

    public async Task<IReadOnlyList<FinancialAccountResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var accounts = await repository.GetAllAsync(cancellationToken);

        return accounts
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .Select(Map)
            .ToList();
    }

    public async Task<FinancialAccountResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(id, cancellationToken);

        return account is { IsActive: true } ? Map(account) : null;
    }

    public async Task<FinancialAccountResponse?> UpdateAsync(
        Guid id,
        UpdateFinancialAccountRequest request,
        CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(id, cancellationToken);

        if (account is not { IsActive: true })
        {
            return null;
        }

        account.Update(request.Name, request.Type, request.InstitutionName);
        await repository.SaveChangesAsync(cancellationToken);

        return Map(account);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await repository.GetByIdAsync(id, cancellationToken);

        if (account is not { IsActive: true })
        {
            return false;
        }

        account.Deactivate();
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static FinancialAccountResponse Map(FinancialAccount account) =>
        new(
            account.Id,
            account.Name,
            account.Type,
            account.InstitutionName,
            account.IsActive,
            account.CreatedAt,
            account.UpdatedAt);
}
