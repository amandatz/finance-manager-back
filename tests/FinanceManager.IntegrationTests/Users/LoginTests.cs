using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Households.Requests;
using FinanceManager.Application.Households.Responses;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Users;

public class LoginTests : IDisposable
{
    private const string Route = "/api/v1/auth/login";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithClaims()
    {
        var client = _factory.CreateClient();
        await _factory.EnsureOwnerRegisteredAsync(client);

        var response = await client.PostAsJsonAsync(
            Route,
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, UserHouseholdApiFactory.SeedOwnerPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.NameIdentifier);
        Assert.Contains(token.Claims, c => c.Type == "household_id");
        Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Owner");
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Route,
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, "WrongPassword123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401WithSameShape()
    {
        var client = _factory.CreateClient();

        var wrongPasswordResponse = await client.PostAsJsonAsync(
            Route,
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, "WrongPassword123"));
        var unknownEmailResponse = await client.PostAsJsonAsync(
            Route,
            new LoginRequest("nobody@test.local", "WhateverPassword123"));

        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmailResponse.StatusCode);

        // Compare the meaningful fields, not the raw body: ProblemDetails carries a
        // per-request `traceId` correlation extension that legitimately differs on every
        // request regardless of outcome - it is not part of "can't tell which one failed".
        var wrongPasswordProblem = await wrongPasswordResponse.Content
            .ReadFromJsonAsync<ProblemDetails>(TestJsonOptions.Default);
        var unknownEmailProblem = await unknownEmailResponse.Content
            .ReadFromJsonAsync<ProblemDetails>(TestJsonOptions.Default);

        Assert.Equal(wrongPasswordProblem!.Status, unknownEmailProblem!.Status);
        Assert.Equal(wrongPasswordProblem.Title, unknownEmailProblem.Title);
    }

    [Fact]
    public async Task Login_IssuedToken_ExpiresIn12Hours()
    {
        var client = _factory.CreateClient();
        await _factory.EnsureOwnerRegisteredAsync(client);

        var response = await client.PostAsJsonAsync(
            Route,
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, UserHouseholdApiFactory.SeedOwnerPassword));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);

        var expectedExpiry = DateTime.UtcNow.AddHours(12);
        Assert.True(
            Math.Abs((token.ValidTo - expectedExpiry).TotalMinutes) < 5,
            $"Expected expiry near {expectedExpiry:O}, got {token.ValidTo:O}");
    }

    [Fact]
    public async Task Login_WithMultipleMemberships_ActivatesEarliestCreated()
    {
        var client = _factory.CreateClient();
        var ownerToken = await _factory.LoginAsOwnerAsync(client);
        var ownerClient = _factory.CreateAuthenticatedClient(ownerToken);
        var firstMembershipHouseholdId = (await (await ownerClient.GetAsync("/api/v1/users/me"))
            .Content.ReadFromJsonAsync<CurrentUserResponse>(TestJsonOptions.Default))!.HouseholdId;

        var secondOwnerClient = _factory.CreateClient();
        var registerResponse = await secondOwnerClient.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(
                "Second Owner", "second-owner-login@test.local", "SecondOwnerPassword123", null, "Second Household"));
        var secondOwnerTokens = await registerResponse.Content
            .ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var authedSecondClient = _factory.CreateAuthenticatedClient(secondOwnerTokens!.AccessToken);
        var invite = await (await authedSecondClient.PostAsync("/api/v1/households/invite-code", null))
            .Content.ReadFromJsonAsync<InviteCodeResponse>(TestJsonOptions.Default);

        // The seed owner's first membership (their own household, created at registration)
        // predates the one they're about to join - it must stay the one login activates.
        await ownerClient.PostAsJsonAsync("/api/v1/households/join", new JoinHouseholdRequest(invite!.InviteCode!));

        var response = await client.PostAsJsonAsync(
            Route,
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, UserHouseholdApiFactory.SeedOwnerPassword));

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(body!.AccessToken);
        Assert.Equal(
            firstMembershipHouseholdId.ToString(),
            token.Claims.First(c => c.Type == "household_id").Value);
    }
}
