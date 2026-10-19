using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Households;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Users;

public class RegisterTests : IDisposable
{
    private const string Route = "/api/v1/auth/register";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_WithoutInviteCode_CreatesHouseholdAndOwnerMembership()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("New Owner", "new-owner@test.local", "NewOwnerPassword123", null, "New Household"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Owner");

        var authedClient = _factory.CreateAuthenticatedClient(body.AccessToken);
        var memberships = await (await authedClient.GetAsync("/api/v1/users/me/households"))
            .Content.ReadFromJsonAsync<List<MembershipResponse>>(TestJsonOptions.Default);
        Assert.Single(memberships!);
    }

    [Fact]
    public async Task Register_WithValidInviteCode_JoinsExistingHouseholdAsMember()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var invite = await (await ownerClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Joiner", "joiner@test.local", "JoinerPassword123", invite!.InviteCode, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Member");

        var ownerProfile = await (await ownerClient.GetAsync("/api/v1/users/me"))
            .Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);
        var joinerClient = _factory.CreateAuthenticatedClient(body.AccessToken);
        var joinerProfile = await (await joinerClient.GetAsync("/api/v1/users/me"))
            .Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);
        Assert.Equal(ownerProfile!.HouseholdId, joinerProfile!.HouseholdId);
    }

    [Fact]
    public async Task Register_WithValidInviteCode_ConsumesCodeSingleUse()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var invite = await (await ownerClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        await client.PostAsJsonAsync(
            Route, new RegisterRequest("First", "first@test.local", "FirstPassword123", invite!.InviteCode, null));
        var second = await client.PostAsJsonAsync(
            Route, new RegisterRequest("Second", "second@test.local", "SecondPassword123", invite.InviteCode, null));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidInviteCode_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Nobody", "nobody@test.local", "NobodyPassword123", "NOTAVALID", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest(
                "Dup", UserHouseholdApiFactory.SeedOwnerEmail, "DupPassword123", null, "Another Household"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Short Pw", "shortpw@test.local", "Sh0rt!", null, "Short Pw Household"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithoutInviteCodeAndNoHouseholdName_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("No Household", "no-household@test.local", "NoHouseholdPassword123", null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("HouseholdName", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Register_WithMalformedEmail_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Bad Email", "not-an-email", "BadEmailPassword123", null, "Bad Email Household"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Email", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Register_WithEmptyName_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest(string.Empty, "empty-name@test.local", "EmptyNamePassword123", null, "Household"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Api_WithNoSeedConfiguration_StartsSuccessfully()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Startup Check", "startup-check@test.local", "StartupCheckPassword123", null, "Startup Household"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
