using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Application.Transactions.Requests;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Transactions;

public class AccountBalanceTests : IDisposable
{
    private const string AccountsRoute = "/api/v1/financial-accounts";
    private const string TransactionsRoute = "/api/v1/transactions";

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

    private static async Task<decimal> GetBalanceAsync(HttpClient client, Guid accountId)
    {
        var response = await client.GetAsync($"{AccountsRoute}/{accountId}");
        var body = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);
        return body!.Balance;
    }

    [Fact]
    public async Task NewAccount_WithNoTransactions_HasZeroBalance()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));

        var balance = await GetBalanceAsync(client, account.Id);

        Assert.Equal(0m, balance);
    }

    [Fact]
    public async Task RecordingExpense_DecreasesBalance()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                account.Id, null, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null));

        var balance = await GetBalanceAsync(client, account.Id);

        Assert.Equal(-50m, balance);
    }

    [Fact]
    public async Task RecordingIncome_IncreasesBalance()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));

        await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                account.Id, null, category.Id, TransactionType.Income, 1000m, "BRL", new DateOnly(2026, 11, 16), null, null));

        var balance = await GetBalanceAsync(client, account.Id);

        Assert.Equal(1000m, balance);
    }

    [Fact]
    public async Task EditingTransactionAmount_UpdatesBalance()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var createResponse = await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                account.Id, null, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);

        await client.PutAsJsonAsync(
            $"{TransactionsRoute}/{created!.Id}",
            new UpdateTransactionRequest(
                category.Id, TransactionType.Expense, 80m, "BRL", new DateOnly(2026, 11, 16), null, null));

        var balance = await GetBalanceAsync(client, account.Id);

        Assert.Equal(-80m, balance);
    }

    [Fact]
    public async Task DeletingTransaction_RemovesItFromBalance()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var createResponse = await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                account.Id, null, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);

        await client.DeleteAsync($"{TransactionsRoute}/{created!.Id}");

        var balance = await GetBalanceAsync(client, account.Id);

        Assert.Equal(0m, balance);
    }

    [Fact]
    public async Task Balance_OnlyConsidersTransactionsForThatAccount()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account1 = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var account2 = await _factory.SeedAccountAsync(
            new FinancialAccount("Savings", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));

        await client.PostAsJsonAsync(
            TransactionsRoute,
            new CreateTransactionRequest(
                account1.Id, null, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null));

        var balance1 = await GetBalanceAsync(client, account1.Id);
        var balance2 = await GetBalanceAsync(client, account2.Id);

        Assert.Equal(-50m, balance1);
        Assert.Equal(0m, balance2);
    }
}
