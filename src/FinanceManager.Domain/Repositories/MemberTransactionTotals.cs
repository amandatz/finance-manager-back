namespace FinanceManager.Domain.Repositories;

/// <summary>
/// Per-member income and household-expense sums for a period, plus whether each side had any
/// matching transaction at all - the presence flags are what let a caller distinguish "recorded
/// zero" (impossible, <see cref="Domain.Entities.Transaction.Amount"/> is always positive) from
/// "recorded nothing" for the household expense split report's incomplete-data signal.
/// </summary>
public sealed record MemberTransactionTotals(
    Guid UserId,
    decimal Income,
    bool HasIncome,
    decimal HouseholdExpense,
    bool HasHouseholdExpense);
