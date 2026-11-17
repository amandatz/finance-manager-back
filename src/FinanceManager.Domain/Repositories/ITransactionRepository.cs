using FinanceManager.Domain.Entities;

namespace FinanceManager.Domain.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetAllAsync(
        TransactionFilter filter,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sum of active income transactions minus active expense transactions for the given account.
    /// </summary>
    Task<decimal> GetAccountBalanceAsync(
        Guid financialAccountId,
        CancellationToken cancellationToken);

    Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
