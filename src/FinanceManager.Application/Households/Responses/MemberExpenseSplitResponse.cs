namespace FinanceManager.Application.Households.Responses;

/// <summary>
/// Null income/expense/expected/deviation fields mean the member has incomplete data for the
/// period (<see cref="HasIncompleteData"/> true) - never a silent zero. Share fields are
/// fractions (e.g. 0.63 for 63%), left for the caller to format.
/// </summary>
public sealed record MemberExpenseSplitResponse(
    Guid UserId,
    string Name,
    decimal? Income,
    decimal? IncomeShare,
    decimal? HouseholdExpense,
    decimal? HouseholdExpenseShare,
    decimal? ExpectedHouseholdExpense,
    decimal? Deviation,
    decimal? DeviationShare,
    bool HasIncompleteData);
