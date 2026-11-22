using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests.Users;

public class RegisterTests : IDisposable
{
    private const string Route = "/api/v1/auth/register";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Register_WithValidData_CreatesUserWithNoHouseholdAndReturnsNullHouseholdId()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("New Owner", "new-owner@test.local", "NewOwnerPassword123"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        Assert.Null(body!.HouseholdId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.DoesNotContain(token.Claims, c => c.Type == "household_id");
        Assert.DoesNotContain(token.Claims, c => c.Type == ClaimTypes.Role);

        var authedClient = _factory.CreateAuthenticatedClient(body.AccessToken);
        var memberships = await (await authedClient.GetAsync("/api/v1/users/me/households"))
            .Content.ReadFromJsonAsync<List<MembershipResponse>>(TestJsonOptions.Default);
        Assert.Empty(memberships!);
    }

    [Fact]
    public async Task Register_ThenCreateHousehold_ActivatesCallerAsOwner()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("New Owner", "new-owner-create@test.local", "NewOwnerPassword123"));
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var pendingClient = _factory.CreateAuthenticatedClient(registerBody!.AccessToken);

        var response = await pendingClient.PostAsJsonAsync(
            "/api/v1/households", new CreateHouseholdRequest("New Household"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        Assert.NotNull(body!.HouseholdId);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Owner");

        var authedClient = _factory.CreateAuthenticatedClient(body.AccessToken);
        var memberships = await (await authedClient.GetAsync("/api/v1/users/me/households"))
            .Content.ReadFromJsonAsync<List<MembershipResponse>>(TestJsonOptions.Default);
        Assert.Single(memberships!);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        await _factory.EnsureOwnerRegisteredAsync(client);

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Dup", UserHouseholdApiFactory.SeedOwnerEmail, "DupPassword123"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Short Pw", "shortpw@test.local", "Sh0rt!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMalformedEmail_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Bad Email", "not-an-email", "BadEmailPassword123"));

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
            new RegisterRequest(string.Empty, "empty-name@test.local", "EmptyNamePassword123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Name", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Register_WithEmptyPassword_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("No Password", "no-password@test.local", string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("Password", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Api_WithNoSeedConfiguration_StartsSuccessfully()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Startup Check", "startup-check@test.local", "StartupCheckPassword123"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_DoesNotPersistAnotherUser()
    {
        var client = _factory.CreateClient();
        await _factory.EnsureOwnerRegisteredAsync(client);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();
        var userCountBefore = await dbContext.Users.CountAsync();

        var response = await client.PostAsJsonAsync(
            Route,
            new RegisterRequest("Dup No Invite", UserHouseholdApiFactory.SeedOwnerEmail, "DupNoInvitePassword123"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var userCountAfter = await dbContext.Users.CountAsync();
        Assert.Equal(userCountBefore, userCountAfter);
    }
}
