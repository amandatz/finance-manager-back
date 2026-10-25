using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.IntegrationTests.FinancialAccounts;

public class DeleteTests : IDisposable
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

    private async Task<HttpClient> AddHouseholdMemberAsync(HttpClient ownerClient)
    {
        var email = $"member-{Guid.NewGuid()}@test.local";
        const string password = "MemberPassword123";

        var createResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/users",
            new CreateMemberRequest("Household Member", email, password));
        createResponse.EnsureSuccessStatusCode();

        var loginClient = _factory.CreateClient();
        var tokens = await _factory.LoginWithTokensAsync(loginClient, email, password);

        return _factory.CreateAuthenticatedClient(tokens.AccessToken);
    }

    [Fact]
    public async Task Delete_WithoutAuthentication_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithActiveAccount_Returns204AndDeactivates()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId));

        var response = await client.DeleteAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var stored = await _factory.FindAccountAsync(account.Id);
        Assert.NotNull(stored);
        Assert.False(stored!.IsActive);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task Delete_WithUnknownId_Returns404()
    {
        var (client, _, _) = await CreateAuthenticatedOwnerAsync();

        var response = await client.DeleteAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithAlreadyDeactivatedAccount_Returns404()
    {
        var (client, userId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, userId);
        account.Deactivate();
        await _factory.SeedAccountAsync(account);

        var response = await client.DeleteAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AsNonOwnerSameHousehold_Returns404AndDoesNotDeactivate()
    {
        var (ownerClient, ownerId, householdId) = await CreateAuthenticatedOwnerAsync();
        var account = await _factory.SeedAccountAsync(
            new FinancialAccount("Checking", FinancialAccountType.Bank, householdId, ownerId));
        var memberClient = await AddHouseholdMemberAsync(ownerClient);

        var response = await memberClient.DeleteAsync($"{Route}/{account.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var stored = await _factory.FindAccountAsync(account.Id);
        Assert.True(stored!.IsActive);
    }
}
