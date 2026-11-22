using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.Households;

public sealed class HouseholdExpenseSplitService(
    ITransactionRepository transactionRepository,
    IIdentityService identityService)
{
    public async Task<HouseholdExpenseSplitResponse> GetReportAsync(
        Guid householdId,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var dateFrom = new DateOnly(year, month, 1);
        var dateTo = dateFrom.AddMonths(1).AddDays(-1);

        var members = await identityService.GetHouseholdMembersAsync(householdId, cancellationToken);
        var totals = await transactionRepository.GetMemberTotalsAsync(dateFrom, dateTo, cancellationToken);

        var totalIncome = totals.Sum(x => x.Income);
        var totalHouseholdExpense = totals.Sum(x => x.HouseholdExpense);
        var hasSufficientIncomeData = totalIncome > 0;

        var memberResponses = members
            .Select(member => BuildMemberResponse(
                member,
                totals.FirstOrDefault(x => x.UserId == member.UserId),
                hasSufficientIncomeData,
                totalIncome,
                totalHouseholdExpense))
            .ToList();

        return new HouseholdExpenseSplitResponse(
            year,
            month,
            HouseholdExpenseSplitRule.ProportionalToIncome,
            hasSufficientIncomeData,
            totalIncome,
            totalHouseholdExpense,
            memberResponses);
    }

    private static MemberExpenseSplitResponse BuildMemberResponse(
        HouseholdMemberInfo member,
        MemberTransactionTotals? totals,
        bool hasSufficientIncomeData,
        decimal totalIncome,
        decimal totalHouseholdExpense)
    {
        var hasIncome = totals?.HasIncome ?? false;
        var hasHouseholdExpense = totals?.HasHouseholdExpense ?? false;
        var hasIncompleteData = !hasSufficientIncomeData || !hasIncome || !hasHouseholdExpense;

        if (!hasSufficientIncomeData)
        {
            return new MemberExpenseSplitResponse(
                member.UserId, member.Name, null, null, null, null, null, null, null, hasIncompleteData);
        }

        var income = hasIncome ? totals!.Income : (decimal?)null;
        var incomeShare = hasIncome ? totals!.Income / totalIncome : (decimal?)null;

        var householdExpense = hasHouseholdExpense ? totals!.HouseholdExpense : (decimal?)null;
        var householdExpenseShare = hasHouseholdExpense && totalHouseholdExpense > 0
            ? totals!.HouseholdExpense / totalHouseholdExpense
            : (decimal?)null;

        decimal? expected = null;
        decimal? deviation = null;
        decimal? deviationShare = null;

        if (hasIncome && hasHouseholdExpense)
        {
            expected = decimal.Round(incomeShare!.Value * totalHouseholdExpense, 2);
            deviation = householdExpense!.Value - expected.Value;
            deviationShare = deviation / totalHouseholdExpense;
        }

        return new MemberExpenseSplitResponse(
            member.UserId,
            member.Name,
            income,
            incomeShare,
            householdExpense,
            householdExpenseShare,
            expected,
            deviation,
            deviationShare,
            hasIncompleteData);
    }
}
