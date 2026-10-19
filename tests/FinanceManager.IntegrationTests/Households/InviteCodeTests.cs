using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;

namespace FinanceManager.IntegrationTests.Households;

public class InviteCodeTests : IDisposable
{
    private const string Route = "/api/v1/households/invite-code";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

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

        var registerClient = _factory.CreateClient();
        var registerResponse = await registerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Usable", "usable@test.local", "UsablePassword123", body.InviteCode, null));
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
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

        var registerClient = _factory.CreateClient();
        var response = await registerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Stale", "stale@test.local", "StalePassword123", first!.InviteCode, null));

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

        var memberClient = _factory.CreateClient();
        var memberTokens = await memberClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Member", "member-invite@test.local", "MemberPassword123", invite!.InviteCode, null));
        var memberBody = await memberTokens.Content
            .ReadFromJsonAsync<FinanceManager.Application.Users.Responses.LoginResponse>(TestJsonOptions.Default);
        var authedMemberClient = _factory.CreateAuthenticatedClient(memberBody!.AccessToken);

        var response = await authedMemberClient.PostAsync(Route, null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerateInviteCode_WithNoToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync(Route, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
