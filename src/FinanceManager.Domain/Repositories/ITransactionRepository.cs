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

    /// <summary>
    /// Sum of active expense transactions minus active income transactions for the given credit
    /// card - the inverse of <see cref="GetAccountBalanceAsync"/>, since a card is a liability.
    /// </summary>
    Task<decimal> GetCreditCardDebtAsync(
        Guid creditCardId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Both legs of a bill payment, active or not - used to archive/inspect the pair as a whole.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetBySettlementGroupIdAsync(
        Guid settlementGroupId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Active income and household-expense totals per <see cref="Transaction.RecordedByUserId"/>,
    /// for every member who recorded at least one such transaction within <paramref name="dateFrom"/>
    /// and <paramref name="dateTo"/> (inclusive) - used by the household expense split report.
    /// A member with no matching transactions at all is absent from the result, not zero.
    /// </summary>
    Task<IReadOnlyList<MemberTransactionTotals>> GetMemberTotalsAsync(
        DateOnly dateFrom,
        DateOnly dateTo,
        CancellationToken cancellationToken);

    Task AddAsync(
        Transaction transaction,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
