using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Households;

public class CreateHouseholdTests : IDisposable
{
    private const string Route = "/api/v1/households";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> RegisterPendingUserAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest("Pending User", email, "PendingPassword123"));
        var tokens = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);

        return _factory.CreateAuthenticatedClient(tokens!.AccessToken);
    }

    [Fact]
    public async Task Create_ForUserWithNoHousehold_ActivatesCallerAsOwner()
    {
        var client = await RegisterPendingUserAsync("create-household@test.local");

        var response = await client.PostAsJsonAsync(Route, new CreateHouseholdRequest("My Household"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        Assert.NotNull(body!.HouseholdId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.Equal(body.HouseholdId.ToString(), token.Claims.First(c => c.Type == "household_id").Value);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Owner");
    }

    [Fact]
    public async Task Create_ForUserAlreadyWithAHousehold_AddsAdditionalMembershipAsOwner()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var response = await ownerClient.PostAsJsonAsync(Route, new CreateHouseholdRequest("Another Household"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);

        var memberships = await (await ownerClient.GetAsync("/api/v1/users/me/households"))
            .Content.ReadFromJsonAsync<List<MembershipResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, memberships!.Count);
        Assert.Contains(memberships, m => m.HouseholdId == body!.HouseholdId);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400()
    {
        var client = await RegisterPendingUserAsync("create-household-empty@test.local");

        var response = await client.PostAsJsonAsync(Route, new CreateHouseholdRequest(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("HouseholdName", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(Route, new CreateHouseholdRequest("No Auth Household"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterThenCreateHousehold_FullCycle_ActivatesDashboardAccess()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Full Cycle", "full-cycle@test.local", "FullCyclePassword123"));
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        Assert.Null(registerBody!.HouseholdId);

        var pendingClient = _factory.CreateAuthenticatedClient(registerBody.AccessToken);
        var meBeforeHousehold = await pendingClient.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meBeforeHousehold.StatusCode);

        var createResponse = await pendingClient.PostAsJsonAsync(Route, new CreateHouseholdRequest("Full Cycle Household"));
        var createBody = await createResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);

        var activeClient = _factory.CreateAuthenticatedClient(createBody!.AccessToken);
        var meAfterHousehold = await activeClient.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.OK, meAfterHousehold.StatusCode);
        var profile = await meAfterHousehold.Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);
        Assert.Equal(createBody.HouseholdId, profile!.HouseholdId);
    }
}
