using FinanceManager.Domain.Entities;

namespace FinanceManager.Application.FinancialAccounts;

public interface IFinancialAccountRepository
{
    Task<FinancialAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FinancialAccount>> GetAllAsync(
        CancellationToken cancellationToken);

    Task AddAsync(
        FinancialAccount account,
        CancellationToken cancellationToken);

    void Remove(FinancialAccount account);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}