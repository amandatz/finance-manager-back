using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;

namespace FinanceManager.IntegrationTests.Households;

public class JoinHouseholdTests : IDisposable
{
    private const string Route = "/api/v1/households/join";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<(HttpClient OwnerClient, string InviteCode)> CreateSecondHouseholdAsync()
    {
        var secondOwnerClient = _factory.CreateClient();
        var registerResponse = await secondOwnerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                "Second Owner", $"second-owner-{Guid.NewGuid()}@test.local", "SecondOwnerPassword123", null,
                "Second Household"));
        var tokens = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var authedClient = _factory.CreateAuthenticatedClient(tokens!.AccessToken);
        var invite = await (await authedClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        return (authedClient, invite!.InviteCode!);
    }

    [Fact]
    public async Task Join_WithValidCode_AddsMembershipAndReturnsTokens()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var (secondHouseholdOwnerClient, inviteCode) = await CreateSecondHouseholdAsync();

        var response = await ownerClient.PostAsJsonAsync(Route, new JoinHouseholdRequest(inviteCode));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokens!.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Member");

        var authedClient = _factory.CreateAuthenticatedClient(ownerToken);
        var memberships = await (await authedClient.GetAsync("/api/v1/users/me/households"))
            .Content.ReadFromJsonAsync<List<MembershipResponse>>(TestJsonOptions.Default);
        Assert.Equal(2, memberships!.Count);
    }

    [Fact]
    public async Task Join_WhenAlreadyAMember_Returns409()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var (_, inviteCode) = await CreateSecondHouseholdAsync();

        await ownerClient.PostAsJsonAsync(Route, new JoinHouseholdRequest(inviteCode));

        // The code was single-use and already consumed, so generate a fresh one for the
        // same household to isolate "already a member" from "code already consumed".
        var secondOwnerClient = _factory.CreateClient();
        var registerResponse = await secondOwnerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                "Third Owner", $"third-owner-{Guid.NewGuid()}@test.local", "ThirdOwnerPassword123", null,
                "Third Household"));
        var tokens = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var thirdOwnerClient = _factory.CreateAuthenticatedClient(tokens!.AccessToken);
        var thirdInvite = await (await thirdOwnerClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        await ownerClient.PostAsJsonAsync(Route, new JoinHouseholdRequest(thirdInvite!.InviteCode!));
        var secondAttemptInvite = await (await thirdOwnerClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        var response = await ownerClient.PostAsJsonAsync(Route, new JoinHouseholdRequest(secondAttemptInvite!.InviteCode!));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Join_WithInvalidCode_Returns400()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var response = await ownerClient.PostAsJsonAsync(Route, new JoinHouseholdRequest("NOTAVALID"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
