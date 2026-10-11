using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Users;

public class CreateMemberTests : IDisposable
{
    private const string Route = "/api/v1/users";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreateMember_AsOwner_Returns201AndAssignsHousehold()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        const string password = "MemberPassword123";

        var response = await ownerClient.PostAsJsonAsync(
            Route,
            new CreateMemberRequest("New Member", "member@test.local", password));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var rawBody = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(password, rawBody);

        var body = JsonSerializer.Deserialize<CreatedUserResponse>(rawBody, TestJsonOptions.Default);
        Assert.Equal("member@test.local", body!.Email);
        Assert.Equal("New Member", body.Name);

        // Prove household assignment through the member's own token, not an internal lookup.
        var memberClient = _factory.CreateClient();
        var memberToken = await _factory.LoginAsync(memberClient, "member@test.local", password);
        var authedMemberClient = _factory.CreateAuthenticatedClient(memberToken);
        var ownerProfile = await (await ownerClient.GetAsync("/api/v1/users/me"))
            .Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);
        var memberProfile = await (await authedMemberClient.GetAsync("/api/v1/users/me"))
            .Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default);

        Assert.Equal(ownerProfile!.HouseholdId, memberProfile!.HouseholdId);
        Assert.Equal(Domain.Enums.HouseholdRole.Member, memberProfile.Role);
    }

    [Fact]
    public async Task CreateMember_AsMember_Returns403()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        await ownerClient.PostAsJsonAsync(
            Route,
            new CreateMemberRequest("Member One", "member-role-test@test.local", "MemberPassword123"));

        var memberClient = _factory.CreateClient();
        var memberToken = await _factory.LoginAsync(
            memberClient, "member-role-test@test.local", "MemberPassword123");
        var authedMemberClient = _factory.CreateAuthenticatedClient(memberToken);

        var response = await authedMemberClient.PostAsJsonAsync(
            Route,
            new CreateMemberRequest("Should Fail", "should-fail@test.local", "SomePassword123"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateMember_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var request = new CreateMemberRequest("Dup", "dup@test.local", "DupPassword123");

        await ownerClient.PostAsJsonAsync(Route, request);
        var secondResponse = await ownerClient.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task CreateMember_WithShortPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var response = await ownerClient.PostAsJsonAsync(
            Route,
            new CreateMemberRequest("Short Pw", "shortpw@test.local", "Sh0rt!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Password", problem!.Errors.Keys);
    }

    [Fact]
    public async Task CreateMember_WithNameOver100Chars_Returns400()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);

        var response = await ownerClient.PostAsJsonAsync(
            Route,
            new CreateMemberRequest(new string('a', 101), "longname@test.local", "SomePassword123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }
}
