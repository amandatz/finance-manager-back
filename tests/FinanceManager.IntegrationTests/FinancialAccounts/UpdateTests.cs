using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.FinancialAccounts.Requests;
using FinanceManager.Application.FinancialAccounts.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class UpdateTests : IDisposable
{
    private const string Route = "/api/v1/financial-accounts";

    private readonly FinancialAccountsApiFactory _factory = new();

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

    /// <summary>
    /// Adds a second member (Member role) to the owner's current household, via
    /// POST /users (same flow CreateMemberTests.cs uses), and returns an authenticated
    /// client plus that member's user id - used to prove a non-owner household member can
    /// view but not mutate an account they don't own.
    /// </summary>
    private async Task<(HttpClient Client, Guid UserId)> AddHouseholdMemberAsync(HttpClient ownerClient)
    {
        var email = $"member-{Guid.NewGuid()}@test.local";
        const string password = "MemberPassword123";

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/users",
            new CreateMemberRequest("Household Member", email, password));
        createResponse.EnsureSuccessStatusCode();

        var loginClient = _factory.CreateClient();
        var tokens = await _factory.LoginWithTokensAsync(loginClient, email, password);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tokens.AccessToken);
        var userId = Guid.Parse(jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        return (_factory.CreateAuthenticatedClient(tokens.AccessToken), userId);
    }

    [Fact]
    public async Task Update_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, null);

        var response = await client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithValidRequest_Returns200WithUpdatedAtSet()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, "New Bank");

        var response = await client.PutAsJsonAsync($"{Route}/{account.Id}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FinancialAccountResponse>(TestJsonOptions.Default);
        Assert.Equal("Savings", body!.Name);
        Assert.NotNull(body.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithEmptyName_Returns400()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));
        var request = new UpdateFinancialAccountRequest(string.Empty, FinancialAccountType.Bank, null);

        var response = await client.PutAsJsonAsync($"{Route}/{account.Id}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithUnknownId_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, null);

        var response = await client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithDeactivatedAccount_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId);
        account.Deactivate();
        await _factory.SeedAccountAsync(account);
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, null);

        var response = await client.PutAsJsonAsync($"{Route}/{account.Id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsNonOwnerSameHousehold_Returns404()
    {
        var (ownerClient, ownerId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var (memberClient, _) = await AddHouseholdMemberAsync(ownerClient);
        var request = new UpdateFinancialAccountRequest("Savings", FinancialAccountType.Bank, null);

        var response = await memberClient.PutAsJsonAsync($"{Route}/{account.Id}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsNonOwnerSameHousehold_Returns200()
    {
        var (ownerClient, ownerId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var (memberClient, _) = await AddHouseholdMemberAsync(ownerClient);

        var response = await memberClient.GetAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
