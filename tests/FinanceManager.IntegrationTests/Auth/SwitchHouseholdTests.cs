using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;

namespace FinanceManager.IntegrationTests.Auth;

public class SwitchHouseholdTests : IDisposable
{
    private const string Route = "/api/v1/auth/switch-household";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Switch_ToOwnMembership_ReturnsTokensScopedToIt()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var ownerProfile = await (await ownerClient.GetAsync("/api/v1/users/me"))
            .Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);

        var invite = await (await ownerClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        var secondHouseholdOwnerClient = _factory.CreateClient();
        var secondOwnerLogin = await secondHouseholdOwnerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                "Second Owner", "second-owner-switch@test.local", "SecondOwnerPassword123", null, "Second Household"));
        var secondOwnerTokens = await secondOwnerLogin.Content
            .ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var secondOwnerClient = _factory.CreateAuthenticatedClient(secondOwnerTokens!.AccessToken);
        var secondInvite = await (await secondOwnerClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        // The seed owner joins the second household as a Member, via an additional
        // membership, without leaving their own household.
        var joinResponse = await ownerClient.PostAsJsonAsync(
            "/api/v1/households/join", new JoinHouseholdRequest(secondInvite!.InviteCode!));
        var joinedTokens = await joinResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var joinedToken = new JwtSecurityTokenHandler().ReadJwtToken(joinedTokens!.AccessToken);
        var joinedHouseholdId = joinedToken.Claims.First(c => c.Type == "household_id").Value;

        var switchBackClient = _factory.CreateAuthenticatedClient(joinedTokens.AccessToken);
        var switchResponse = await switchBackClient.PostAsJsonAsync(
            Route, new SwitchHouseholdRequest(ownerProfile!.HouseholdId));

        Assert.Equal(HttpStatusCode.OK, switchResponse.StatusCode);
        var switched = await switchResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var switchedToken = new JwtSecurityTokenHandler().ReadJwtToken(switched!.AccessToken);
        Assert.Equal(ownerProfile.HouseholdId.ToString(), switchedToken.Claims.First(c => c.Type == "household_id").Value);
        Assert.Contains(switchedToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Owner");
        Assert.NotEqual(joinedHouseholdId, switchedToken.Claims.First(c => c.Type == "household_id").Value);
    }

    [Fact]
    public async Task Switch_ToHouseholdNotAMemberOf_Returns404()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var authedClient = _factory.CreateAuthenticatedClient(ownerToken);

        var response = await authedClient.PostAsJsonAsync(Route, new SwitchHouseholdRequest(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
