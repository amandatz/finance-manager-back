using System.Net;
using System.Net.Http.Json;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.IntegrationTests.Users;

public class RefreshTokenTests : IDisposable
{
    private const string RefreshRoute = "/api/v1/auth/refresh";
    private const string LogoutRoute = "/api/v1/auth/logout";

    private readonly UserHouseholdApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Login_ReturnsAccessAndRefreshToken_StoredAsHash()
    {
        var client = _factory.CreateClient();

        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);

        Assert.False(string.IsNullOrWhiteSpace(tokens.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(tokens.RefreshToken));

        var record = await _factory.FindRefreshTokenAsync(tokens.RefreshToken);
        Assert.NotNull(record);
        Assert.NotEqual(tokens.RefreshToken, record!.TokenHash);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPairAndInvalidatesOld()
    {
        var client = _factory.CreateClient();
        var original = await _factory.LoginAsOwnerWithTokensAsync(client);

        var response = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(original.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rotated = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        Assert.NotNull(rotated);
        Assert.False(string.IsNullOrWhiteSpace(rotated!.AccessToken));
        // The access token's content can legitimately be identical to the original if both
        // are issued within the same second (deterministic claims + expiry + signature) -
        // the refresh token, generated from random bytes, is what's guaranteed to differ.
        Assert.NotEqual(original.RefreshToken, rotated.RefreshToken);

        var replay = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(original.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task Login_IssuedRefreshToken_Expires30DaysFromIssuance()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);

        var record = await _factory.FindRefreshTokenAsync(tokens.RefreshToken);

        var expectedLifetime = TimeSpan.FromDays(30);
        var actualLifetime = record!.ExpiresAt - record.CreatedAt;
        Assert.True(
            Math.Abs((actualLifetime - expectedLifetime).TotalMinutes) < 5,
            $"Expected ~{expectedLifetime}, got {actualLifetime}");
    }

    [Fact]
    public async Task Refresh_ReplayedToken_Returns401AndRevokesFamily()
    {
        var client = _factory.CreateClient();
        var original = await _factory.LoginAsOwnerWithTokensAsync(client);

        var firstRotation = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(original.RefreshToken));
        var rotated = await firstRotation.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);

        var replay = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(original.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);

        // The legitimately-rotated token (rotated.RefreshToken) must be dead too - the
        // whole family was revoked, not just the replayed one.
        var useRotated = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(rotated!.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, useRotated.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_Returns401()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);
        await _factory.ExpireRefreshTokenAsync(tokens.RefreshToken);

        var response = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidToken_RevokesIt()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);

        var logoutResponse = await client.PostAsJsonAsync(LogoutRoute, new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(tokens.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_Twice_IsIdempotent()
    {
        var client = _factory.CreateClient();
        var tokens = await _factory.LoginAsOwnerWithTokensAsync(client);

        var first = await client.PostAsJsonAsync(LogoutRoute, new RefreshRequest(tokens.RefreshToken));
        var second = await client.PostAsJsonAsync(LogoutRoute, new RefreshRequest(tokens.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_Returns204()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(LogoutRoute, new RefreshRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_Returns400()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestJsonOptions.Default);
        Assert.Contains("RefreshToken", problem!.Errors.Keys);
    }
}
