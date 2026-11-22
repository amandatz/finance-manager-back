using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;

namespace FinanceManager.IntegrationTests.Households;

public class InviteCodeTests : IDisposable
{
    private const string Route = "/api/v1/households/invite-code";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private async Task<HttpClient> RegisterAndAuthenticateAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register", new RegisterRequest("Joiner", email, password));
        var tokens = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);

        return _factory.CreateAuthenticatedClient(tokens!.AccessToken);
    }

    [Fact]
    public async Task GenerateInviteCode_AsOwner_ReturnsCodeAndPersistsIt()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var response = await ownerClient.PostAsync(Route, null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);
        Assert.False(string.IsNullOrWhiteSpace(body!.InviteCode));

        var joinerClient = await RegisterAndAuthenticateAsync("usable@test.local", "UsablePassword123");
        var joinResponse = await joinerClient.PostAsJsonAsync(
            "/api/v1/households/join", new JoinHouseholdRequest(body.InviteCode));
        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);
    }

    [Fact]
    public async Task GenerateInviteCode_Regenerated_InvalidatesOldCode()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var first = await (await ownerClient.PostAsync(Route, null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);
        await ownerClient.PostAsync(Route, null);

        var joinerClient = await RegisterAndAuthenticateAsync("stale@test.local", "StalePassword123");
        var response = await joinerClient.PostAsJsonAsync(
            "/api/v1/households/join", new JoinHouseholdRequest(first!.InviteCode!));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInviteCode_AsMember_Returns403()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var invite = await (await ownerClient.PostAsync(Route, null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        var memberClient = await RegisterAndAuthenticateAsync("member-invite@test.local", "MemberPassword123");
        await memberClient.PostAsJsonAsync("/api/v1/households/join", new JoinHouseholdRequest(invite!.InviteCode!));

        var response = await memberClient.PostAsync(Route, null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInviteCode_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(Route, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInviteCode_WithNoActiveHousehold_Returns403()
    {
        // No active household means no role claim either, so [Authorize(Roles = Owner)]
        // denies the request before the controller (and GetHouseholdId) ever runs.
        var client = await _factory.CreateClientWithNoHouseholdTokenAsync();

        var response = await client.PostAsync(Route, null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
