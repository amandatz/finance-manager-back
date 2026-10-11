using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;

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

        var wrongPasswordBody = await wrongPasswordResponse.Content.ReadAsStringAsync();
        var unknownEmailBody = await unknownEmailResponse.Content.ReadAsStringAsync();
        Assert.Equal(wrongPasswordBody, unknownEmailBody);
    }

    [Fact]
    public async Task Login_IssuedToken_ExpiresIn12Hours()
    {
        var client = _factory.CreateClient();

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
}
