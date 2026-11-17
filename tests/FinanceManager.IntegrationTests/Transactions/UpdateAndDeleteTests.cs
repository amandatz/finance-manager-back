using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Transactions.Requests;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.Transactions;

public class UpdateAndDeleteTests : IDisposable
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

    private async Task<HttpClient> AddHouseholdMemberAsync(HttpClient ownerClient)
    {
        var email = $"member-{Guid.NewGuid()}@test.local";
        const string password = "MemberPassword123";

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/users",
            new FinanceManager.Application.Users.Requests.CreateMemberRequest("Household Member", email, password));
        createResponse.EnsureSuccessStatusCode();

        var loginClient = _factory.CreateClient();
        var tokens = await _factory.LoginWithTokensAsync(loginClient, email, password);

        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    [Fact]
    public async Task Update_WithValidRequest_Returns200WithUpdatedAtSetAndRecordedByUnchanged()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var transaction = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 50m, "BRL",
            new DateOnly(2026, 11, 16), householdId));
        var request = new UpdateTransactionRequest(
            category.Id, TransactionType.Expense, 75m, "BRL", new DateOnly(2026, 11, 16), "Corrigido", null);

        var response = await client.PutAsJsonAsync($"{Route}/{transaction.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);
        Assert.Equal(75m, body!.Amount);
        Assert.Equal(userId, body.RecordedByUserId);
        Assert.NotNull(body.UpdatedAt);
    }

    [Fact]
    public async Task Update_AsDifferentHouseholdMember_Returns200AndKeepsOriginalRecordedBy()
    {
        var (ownerClient, ownerId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var transaction = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, ownerId, TransactionType.Expense, 50m, "BRL",
            new DateOnly(2026, 11, 16), householdId));
        var memberClient = await AddHouseholdMemberAsync(ownerClient);
        var request = new UpdateTransactionRequest(
            category.Id, TransactionType.Expense, 75m, "BRL", new DateOnly(2026, 11, 16), "Corrigido pelo parceiro", null);

        var response = await memberClient.PutAsJsonAsync($"{Route}/{transaction.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);
        Assert.Equal(ownerId, body!.RecordedByUserId);
    }

    [Fact]
    public async Task Update_WithTypeMismatchingCategoryType_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var expenseCategory = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var transaction = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, expenseCategory.Id, userId, TransactionType.Expense, 50m, "BRL",
            new DateOnly(2026, 11, 16), householdId));
        var request = new UpdateTransactionRequest(
            incomeCategory.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PutAsJsonAsync($"{Route}/{transaction.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new UpdateTransactionRequest(
            category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithActiveTransaction_Returns204AndDeactivates()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var transaction = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, userId, TransactionType.Expense, 50m, "BRL",
            new DateOnly(2026, 11, 16), householdId));

        var response = await client.DeleteAsync($"{Route}/{transaction.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await _factory.FindTransactionAsync(transaction.Id);
        Assert.False(stored!.IsActive);
    }

    [Fact]
    public async Task Delete_AsDifferentHouseholdMember_Returns204()
    {
        var (ownerClient, ownerId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var transaction = await _factory.SeedTransactionAsync(new Transaction(
            account.Id, category.Id, ownerId, TransactionType.Expense, 50m, "BRL",
            new DateOnly(2026, 11, 16), householdId));
        var memberClient = await AddHouseholdMemberAsync(ownerClient);

        var response = await memberClient.DeleteAsync($"{Route}/{transaction.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithUnknownId_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
