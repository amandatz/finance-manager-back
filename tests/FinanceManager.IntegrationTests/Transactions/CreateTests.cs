using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Transactions.Requests;
using FinanceManager.Application.Transactions.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Transactions;

public class CreateTests : IDisposable
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
    public async Task Create_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new CreateTransactionRequest(
            Guid.NewGuid(), Guid.NewGuid(), TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithValidRequest_Returns201WithLocationAndBody()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), "Supermercado", null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);
        Assert.NotNull(body);
        Assert.Equal(account.Id, body!.FinancialAccountId);
        Assert.Equal(category.Id, body.CategoryId);
        Assert.Equal(userId, body.RecordedByUserId);
        Assert.Equal(50m, body.Amount);
        Assert.True(body.IsActive);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"{Route}/{body.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Create_AsNonOwnerOfAccount_Returns201()
    {
        var (ownerClient, ownerId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var memberClient = await AddHouseholdMemberAsync(ownerClient);
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await memberClient.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);
        Assert.NotEqual(ownerId, body!.RecordedByUserId);
    }

    [Fact]
    public async Task Create_WithZeroAmount_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 0m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Amount", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithNegativeAmount_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, -10m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTwoDecimalPlacesAmount_Returns201()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 10.10m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TransactionResponse>(TestJsonOptions.Default);
        Assert.Equal(10.10m, body!.Amount);
    }

    [Fact]
    public async Task Create_WithMoreThanTwoDecimalPlacesAmount_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 10.126m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Amount", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithNonBrlCurrency_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 50m, "USD", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Currency", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithDescriptionOver250Chars_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), new string('a', 251), null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithTypeMismatchingCategoryType_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var incomeCategory = await _factory.SeedCategoryAsync(new Category("Salário", CategoryType.Income, householdId));
        var request = new CreateTransactionRequest(
            account.Id, incomeCategory.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownFinancialAccountId_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            Guid.NewGuid(), category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithDeactivatedFinancialAccount_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId);
        account.Deactivate();
        await _factory.SeedAccountAsync(account);
        var category = await _factory.SeedCategoryAsync(new Category("Alimentação", CategoryType.Expense, householdId));
        var request = new CreateTransactionRequest(
            account.Id, category.Id, TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUnknownCategoryId_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var request = new CreateTransactionRequest(
            account.Id, Guid.NewGuid(), TransactionType.Expense, 50m, "BRL", new DateOnly(2026, 11, 16), null, null);

        var response = await client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
