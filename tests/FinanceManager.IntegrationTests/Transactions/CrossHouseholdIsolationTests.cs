using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Transactions.Requests;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Transactions;

public class CrossHouseholdIsolationTests : IDisposable
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
    public async Task GetAll_NeverReturnsAnotherHouseholdsTransactions()
    {
        var (householdAClient, _, _) = await CreateAuthenticatedOwnerAsync();
        var (_, householdBOwnerId, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var accountB = await _factory.SeedAccountAsync(
            new FinancialAccount("Household B Checking", FinancialAccountType.Bank, householdBId, householdBOwnerId));
        var categoryB = await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));
        await _factory.SeedTransactionAsync(new Transaction(
            accountB.Id, categoryB.Id, householdBOwnerId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdBId));

        var response = await householdAClient.GetAsync(Route);
        var body = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>(TestJsonOptions.Default);

        Assert.Empty(body!);
    }

    [Fact]
    public async Task GetById_ForAnotherHouseholdsTransaction_Returns404()
    {
        var (householdAClient, _, _) = await CreateAuthenticatedOwnerAsync();
        var (_, householdBOwnerId, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var accountB = await _factory.SeedAccountAsync(
            new FinancialAccount("Household B Checking", FinancialAccountType.Bank, householdBId, householdBOwnerId));
        var categoryB = await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));
        var transactionB = await _factory.SeedTransactionAsync(new Transaction(
            accountB.Id, categoryB.Id, householdBOwnerId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdBId));

        var response = await householdAClient.GetAsync($"{Route}/{transactionB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_ForAnotherHouseholdsTransaction_Returns404AndDoesNotMutate()
    {
        var (householdAClient, _, _) = await CreateAuthenticatedOwnerAsync();
        var (_, householdBOwnerId, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var accountB = await _factory.SeedAccountAsync(
            new FinancialAccount("Household B Checking", FinancialAccountType.Bank, householdBId, householdBOwnerId));
        var categoryB = await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));
        var transactionB = await _factory.SeedTransactionAsync(new Transaction(
            accountB.Id, categoryB.Id, householdBOwnerId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdBId));
        var request = new UpdateTransactionRequest(
            categoryB.Id, TransactionType.Expense, 9999m, "BRL", new DateOnly(2026, 11, 1), "Hijacked", null);

        var response = await householdAClient.PutAsJsonAsync($"{Route}/{transactionB.Id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await _factory.FindTransactionAsync(transactionB.Id);
        Assert.Equal(10m, stored!.Amount);
    }

    [Fact]
    public async Task Delete_ForAnotherHouseholdsTransaction_Returns404AndDoesNotDeactivate()
    {
        var (householdAClient, _, _) = await CreateAuthenticatedOwnerAsync();
        var (_, householdBOwnerId, householdBId) = await _factory.RegisterNewHouseholdOwnerAsync("Household B");
        var accountB = await _factory.SeedAccountAsync(
            new FinancialAccount("Household B Checking", FinancialAccountType.Bank, householdBId, householdBOwnerId));
        var categoryB = await _factory.SeedCategoryAsync(new Category("Household B Category", CategoryType.Expense, householdBId));
        var transactionB = await _factory.SeedTransactionAsync(new Transaction(
            accountB.Id, categoryB.Id, householdBOwnerId, TransactionType.Expense, 10m, "BRL",
            new DateOnly(2026, 11, 1), householdBId));

        var response = await householdAClient.DeleteAsync($"{Route}/{transactionB.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await _factory.FindTransactionAsync(transactionB.Id);
        Assert.True(stored!.IsActive);
    }
}
