using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Transactions;

public class ListTests : IDisposable
{
    private const string Route = "/api/v1/transactions";

    private readonly LedgerApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient Client, Guid UserId, Guid HouseholdId)> CreateAuthenticatedOwnerAsync()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var userId = Guid.Parse(jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        var householdId = Guid.Parse(jwt.Claims.First(c => c.Type == "household_id").Value);

        return (_factory.CreateAuthenticatedClient(tokens.AccessToken), userId, householdId);
    }

    [Fact]
    public async Task GetAll_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithNoFilter_ReturnsAllActiveOrderedByDateDescending()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        var older = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdId));
        var newer = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 20m, "BRL",
            new DateOnly(2026, 11, 15), householdId));

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        Assert.Equal([newer.Id, older.Id], body!.Select(x => x.Id));
    }

    [Fact]
    public async Task GetAll_WithDeactivatedTransaction_ExcludesIt()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        var active = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdId));

        var deactivated = new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 20m, "BRL",
            new DateOnly(2026, 11, 1), householdId);
        deactivated.Deactivate();
        await _factory.SeedTransactionAsync(deactivated);

        var response = await client.GetAsync(Route);

        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        var transaction = Assert.Single(body!);
        Assert.Equal(active.Id, transaction.Id);
    }

    [Fact]
    public async Task GetAll_FilteredByAccountId_ReturnsOnlyThatAccountsTransactions()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account1 = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var account2 = await _factory.SeedAccountAsync(
            new FinancialAccount("Savings", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        var match = await _factory.SeedTransactionAsync(new Transaction(
            account1.Id, category.Id, userId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account2.Id, category.Id, userId, TransactionType.Expense, 20m, "BRL",
            new DateOnly(2026, 11, 1), householdId));

        var response = await client.GetAsync($"{Route}?accountId={account1.Id}");

        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        var transaction = Assert.Single(body!);
        Assert.Equal(match.Id, transaction.Id);
    }

    [Fact]
    public async Task GetAll_FilteredByCategoryId_ReturnsOnlyThatCategorysTransactions()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category1 = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var category2 = await _factory.SeedCategoryAsync(new Category("Transporte", CategoryType.Expense, householdId));

        var match = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category1.Id, userId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category2.Id, userId, TransactionType.Expense, 20m, "BRL",
            new DateOnly(2026, 11, 1), householdId));

        var response = await client.GetAsync($"{Route}?categoryId={category1.Id}");

        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        var transaction = Assert.Single(body!);
        Assert.Equal(match.Id, transaction.Id);
    }

    [Fact]
    public async Task GetAll_FilteredByType_ReturnsOnlyMatchingType()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var expenseCategory = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        var match = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, incomeCategory.Id, userId, TransactionType.Income, 1000m, "BRL",
            new DateOnly(2026, 11, 1), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, expenseCategory.Id, userId, TransactionType.Expense, 20m, "BRL",
            new DateOnly(2026, 11, 1), householdId));

        var response = await client.GetAsync($"{Route}?type={(int)TransactionType.Income}");

        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        var transaction = Assert.Single(body!);
        Assert.Equal(match.Id, transaction.Id);
    }

    [Fact]
    public async Task GetAll_FilteredByDateRange_ReturnsOnlyTransactionsWithinRange()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 10, 1), householdId));
        var inRange = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 20m, "BRL",
            new DateOnly(2026, 11, 10), householdId));
        await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 30m, "BRL",
            new DateOnly(2026, 12, 1), householdId));

        var response = await client.GetAsync($"{Route}?dateFrom=2026-11-01&dateTo=2026-11-30");

        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        var transaction = Assert.Single(body!);
        Assert.Equal(inRange.Id, transaction.Id);
    }

    [Fact]
    public async Task GetAll_WithNoTransactions_ReturnsEmptyArray()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.GetAsync(Route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);
        Assert.Empty(body!);
    }
}
