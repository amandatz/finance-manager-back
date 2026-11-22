namespace FinanceManager.Domain.Enums;

/// <summary>
/// How the expected share of household expenses is derived for each member. Only one value
/// exists today, but naming the rule keeps the door open for others (e.g. a fixed 50/50 split)
/// without a breaking change to callers.
/// </summary>
public enum HouseholdExpenseSplitRule
{
    ProportionalToIncome,
}
