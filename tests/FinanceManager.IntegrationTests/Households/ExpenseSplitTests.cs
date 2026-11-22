using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Households;

public class ExpenseSplitTests : IDisposable
{
    private const string Route = "/api/v1/households/expense-split";

    private readonly LedgerApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetExpenseSplit_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"{Route}?period=2026-11");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetExpenseSplit_WithInvalidPeriodFormat_Returns400()
    {
        var (client, _, _) = await _factory.CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync($"{Route}?period=not-a-period");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetExpenseSplit_WithNoIncomeRecorded_ReturnsInsufficientIncomeData()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(
            new Category("Luz", CategoryType.Expense, householdId, defaultIsHouseholdExpense: true));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 100m, "BRL",
            new DateOnly(2026, 11, 10), householdId, isHouseholdExpense: true));

        var response = await client.GetAsync($"{Route}?period=2026-11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);
        Assert.False(body!.HasSufficientIncomeData);
        Assert.Equal(0m, body.TotalIncome);
        Assert.All(body.Members, m => Assert.True(m.HasIncompleteData));
        Assert.All(body.Members, m => Assert.Null(m.Income));
    }

    [Fact]
    public async Task GetExpenseSplit_WithTwoMembersProportionalIncome_ComputesExpectedAndDeviation()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var (memberClient, memberId) = await _factory.AddHouseholdMemberAsync(ownerClient);
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var expenseCategory = await _factory.SeedCategoryAsync(
            new Category("Luz", CategoryType.Expense, householdId, defaultIsHouseholdExpense: true));

        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, ownerId, TransactionType.Income, 6000m, "BRL",
            new DateOnly(2026, 11, 5), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, memberId, TransactionType.Income, 4000m, "BRL",
            new DateOnly(2026, 11, 5), householdId));

        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, expenseCategory.Id, ownerId, TransactionType.Expense, 500m, "BRL",
            new DateOnly(2026, 11, 10), householdId, isHouseholdExpense: true));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, expenseCategory.Id, memberId, TransactionType.Expense, 500m, "BRL",
            new DateOnly(2026, 11, 10), householdId, isHouseholdExpense: true));

        var response = await ownerClient.GetAsync($"{Route}?period=2026-11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);
        Assert.True(body!.HasSufficientIncomeData);
        Assert.Equal(10000m, body.TotalIncome);
        Assert.Equal(1000m, body.TotalHouseholdExpense);
        Assert.Equal(2, body.Members.Count);

        var owner = body.Members.Single(m => m.UserId == ownerId);
        Assert.Equal(6000m, owner.Income);
        Assert.Equal(0.6m, owner.IncomeShare);
        Assert.Equal(500m, owner.HouseholdExpense);
        Assert.Equal(600m, owner.ExpectedHouseholdExpense);
        Assert.Equal(-100m, owner.Deviation);
        Assert.False(owner.HasIncompleteData);

        var member = body.Members.Single(m => m.UserId == memberId);
        Assert.Equal(4000m, member.Income);
        Assert.Equal(0.4m, member.IncomeShare);
        Assert.Equal(400m, member.ExpectedHouseholdExpense);
        Assert.Equal(100m, member.Deviation);
        Assert.False(member.HasIncompleteData);
    }

    [Fact]
    public async Task GetExpenseSplit_MemberWithNoIncome_IsMarkedIncompleteButOthersAreNot()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var (_, memberId) = await _factory.AddHouseholdMemberAsync(ownerClient);
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var expenseCategory = await _factory.SeedCategoryAsync(
            new Category("Luz", CategoryType.Expense, householdId, defaultIsHouseholdExpense: true));

        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, ownerId, TransactionType.Income, 5000m, "BRL",
            new DateOnly(2026, 11, 5), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, expenseCategory.Id, ownerId, TransactionType.Expense, 500m, "BRL",
            new DateOnly(2026, 11, 10), householdId, isHouseholdExpense: true));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, expenseCategory.Id, memberId, TransactionType.Expense, 300m, "BRL",
            new DateOnly(2026, 11, 10), householdId, isHouseholdExpense: true));

        var response = await ownerClient.GetAsync($"{Route}?period=2026-11");

        var body = await response.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);
        Assert.True(body!.HasSufficientIncomeData);

        var owner = body.Members.Single(m => m.UserId == ownerId);
        Assert.False(owner.HasIncompleteData);

        var member = body.Members.Single(m => m.UserId == memberId);
        Assert.True(member.HasIncompleteData);
        Assert.Null(member.Income);
        Assert.Null(member.ExpectedHouseholdExpense);
        Assert.Null(member.Deviation);
        Assert.Equal(300m, member.HouseholdExpense);
    }

    [Fact]
    public async Task GetExpenseSplit_MemberWithNoHouseholdExpense_IsMarkedIncomplete()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var (_, memberId) = await _factory.AddHouseholdMemberAsync(ownerClient);
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var expenseCategory = await _factory.SeedCategoryAsync(
            new Category("Luz", CategoryType.Expense, householdId, defaultIsHouseholdExpense: true));

        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, ownerId, TransactionType.Income, 5000m, "BRL",
            new DateOnly(2026, 11, 5), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, memberId, TransactionType.Income, 3000m, "BRL",
            new DateOnly(2026, 11, 5), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, expenseCategory.Id, ownerId, TransactionType.Expense, 500m, "BRL",
            new DateOnly(2026, 11, 10), householdId, isHouseholdExpense: true));

        var response = await ownerClient.GetAsync($"{Route}?period=2026-11");

        var body = await response.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);

        var member = body!.Members.Single(m => m.UserId == memberId);
        Assert.True(member.HasIncompleteData);
        Assert.Equal(3000m, member.Income);
        Assert.Null(member.HouseholdExpense);
        Assert.Null(member.ExpectedHouseholdExpense);
        Assert.Null(member.Deviation);
    }

    [Fact]
    public async Task GetExpenseSplit_WithThreeMembers_DistributesAmongAll()
    {
        var (ownerClient, ownerId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var (_, member1Id) = await _factory.AddHouseholdMemberAsync(ownerClient);
        var (_, member2Id) = await _factory.AddHouseholdMemberAsync(ownerClient);
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var expenseCategory = await _factory.SeedCategoryAsync(
            new Category("Aluguel", CategoryType.Expense, householdId, defaultIsHouseholdExpense: true));

        foreach (var userId in new[] { ownerId, member1Id, member2Id })
        {
            await _factory.SeedTransactionAsync(new Transaction(
                account.Id, incomeCategory.Id, userId, TransactionType.Income, 3000m, "BRL",
                new DateOnly(2026, 11, 5), householdId));
            await _factory.SeedTransactionAsync(new Transaction(
                account.Id, expenseCategory.Id, userId, TransactionType.Expense, 300m, "BRL",
                new DateOnly(2026, 11, 10), householdId, isHouseholdExpense: true));
        }

        var response = await ownerClient.GetAsync($"{Route}?period=2026-11");

        var body = await response.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);
        Assert.Equal(3, body!.Members.Count);
        Assert.All(body.Members, m => Assert.Equal(0m, m.Deviation));
        Assert.All(body.Members, m => Assert.False(m.HasIncompleteData));
    }

    [Fact]
    public async Task GetExpenseSplit_DifferentPeriods_DoNotMixData()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var expenseCategory = await _factory.SeedCategoryAsync(
            new Category("Luz", CategoryType.Expense, householdId, defaultIsHouseholdExpense: true));

        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, userId, TransactionType.Income, 1000m, "BRL",
            new DateOnly(2026, 10, 5), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, userId, TransactionType.Income, 2000m, "BRL",
            new DateOnly(2026, 11, 5), householdId));

        var octoberResponse = await client.GetAsync($"{Route}?period=2026-10");
        var novemberResponse = await client.GetAsync($"{Route}?period=2026-11");

        var octoberBody = await octoberResponse.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);
        var novemberBody = await novemberResponse.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);

        Assert.Equal(1000m, octoberBody!.TotalIncome);
        Assert.Equal(2000m, novemberBody!.TotalIncome);
    }

    [Fact]
    public async Task GetExpenseSplit_NeverIncludesAnotherHouseholdsData()
    {
        var (householdAClient, householdAUserId, householdAId) = await _factory.CreateAuthenticatedOwnerAsync();
        var accountA = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking A", FinancialAccountType.Bank, householdAId, householdAUserId));
        var incomeCategoryA = await _factory.SeedCategoryAsync(new Category("Salário A", CategoryType.Income, householdAId));
        await _factory.SeedTransactionAsync(new Transaction(
            accountA.Id, incomeCategoryA.Id, householdAUserId, TransactionType.Income, 1000m, "BRL",
            new DateOnly(2026, 11, 5), householdAId));

        var (_, householdBOwnerId, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var accountB = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking B", FinancialAccountType.Bank, householdBId, householdBOwnerId));
        var incomeCategoryB = await _factory.SeedCategoryAsync(new Category("Salário B", CategoryType.Income, householdBId));
        await _factory.SeedTransactionAsync(new Transaction(
            accountB.Id, incomeCategoryB.Id, householdBOwnerId, TransactionType.Income, 50000m, "BRL",
            new DateOnly(2026, 11, 5), householdBId));

        var response = await householdAClient.GetAsync($"{Route}?period=2026-11");

        var body = await response.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);
        Assert.Equal(1000m, body!.TotalIncome);
        Assert.DoesNotContain(body.Members, m => m.UserId == householdBOwnerId);
    }

    [Fact]
    public async Task GetExpenseSplit_IgnoresInactiveTransactions()
    {
        var (client, userId, householdId) = await _factory.CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));

        var deactivated = new Transaction(
            account.Id, incomeCategory.Id, userId, TransactionType.Income, 9999m, "BRL",
            new DateOnly(2026, 11, 5), householdId);
        deactivated.Deactivate();
        await _factory.SeedTransactionAsync(deactivated);
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, userId, TransactionType.Income, 1000m, "BRL",
            new DateOnly(2026, 11, 5), householdId));

        var response = await client.GetAsync($"{Route}?period=2026-11");

        var body = await response.Content.ReadFromJsonAsync<HouseholdExpenseSplitResponse>(TestJsonOptions.Default);
        Assert.Equal(1000m, body!.TotalIncome);
    }
}
