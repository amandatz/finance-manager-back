using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Households.Responses;

/// <summary>
/// <see cref="HasSufficientIncomeData"/> false means no member recorded any income for the
/// period - proportions cannot be computed, and <see cref="Members"/> carries no per-member
/// income/expense breakdown in that case (every member's fields are null, see
/// <see cref="MemberExpenseSplitResponse"/>).
/// </summary>
public sealed record HouseholdExpenseSplitResponse(
    int Year,
    int Month,
    HouseholdExpenseSplitRule Rule,
    bool HasSufficientIncomeData,
    decimal TotalIncome,
    decimal TotalHouseholdExpense,
    IReadOnlyList<MemberExpenseSplitResponse> Members);
