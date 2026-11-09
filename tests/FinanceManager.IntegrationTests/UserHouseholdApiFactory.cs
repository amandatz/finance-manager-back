using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Infrastructure.Identity;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests;

public class UserHouseholdApiFactory : InMemoryDbApiFactory
{
    public const string SeedOwnerEmail = "owner@test.local";
    public const string SeedOwnerPassword = "TestPassword123";
    public const string SeedOwnerName = "Test Owner";
    public const string SeedHouseholdName = "Test Household";

    private bool _ownerRegistered;

    protected override void ConfigureAppConfiguration(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-characters-long-for-hmac",
                ["Jwt:Issuer"] = "FinanceManager.Tests",
            });
        });
    }

    public async Task<LoginResponse> LoginWithTokensAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default))!;
    }

    public async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var tokens = await LoginWithTokensAsync(client, email, password);
        return tokens.AccessToken;
    }

    public async Task<string> LoginAsOwnerAsync(HttpClient client)
    {
        var tokens = await LoginAsOwnerWithTokensAsync(client);
        return tokens.AccessToken;
    }

    public async Task<LoginResponse> LoginAsOwnerWithTokensAsync(HttpClient client)
    {
        await EnsureOwnerRegisteredAsync(client);
        return await LoginWithTokensAsync(client, SeedOwnerEmail, SeedOwnerPassword);
    }

    public async Task EnsureOwnerRegisteredAsync(HttpClient client)
    {
        if (_ownerRegistered)
        {
            return;
        }

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest(SeedOwnerName, SeedOwnerEmail, SeedOwnerPassword, null, SeedHouseholdName));

        response.EnsureSuccessStatusCode();
        _ownerRegistered = true;
    }

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>
    /// Logs in as the seed owner, then mints a fresh access token for the same user but
    /// with no <c>household_id</c>/role claims - simulating a user with no active household
    /// (e.g. removed from every household), which the real token issuer also produces for.
    /// </summary>
    public async Task<HttpClient> CreateClientWithNoHouseholdTokenAsync()
    {
        var loginClient = CreateClient();
        var ownerToken = await LoginAsOwnerAsync(loginClient);
        var userId = Guid.Parse(
            new JwtSecurityTokenHandler().ReadJwtToken(ownerToken).Claims
                .First(c => c.Type == ClaimTypes.NameIdentifier).Value);

        using var scope = Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<ITokenGenerator>();
        var tokenWithoutHousehold = tokenGenerator.GenerateAccessToken(userId, householdId: null, role: null);

        return CreateAuthenticatedClient(tokenWithoutHousehold);
    }

    public async Task<RefreshToken?> FindRefreshTokenAsync(string rawToken)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();
        var hash = RefreshTokenService.ComputeHash(rawToken);

        return await db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash);
    }

    public async Task ExpireRefreshTokenAsync(string rawToken)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();
        var hash = RefreshTokenService.ComputeHash(rawToken);

        var record = await db.RefreshTokens.FirstAsync(t => t.TokenHash == hash);
        record.ExpiresAt = DateTime.UtcNow.AddDays(-1);

        await db.SaveChangesAsync();
    }
}
