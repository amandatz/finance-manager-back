using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace FinanceManager.IntegrationTests;

public class UserHouseholdApiFactory : InMemoryDbApiFactory
{
    public const string SeedOwnerEmail = "owner@test.local";
    public const string SeedOwnerPassword = "TestPassword123";
    public const string SeedOwnerName = "Test Owner";
    public const string SeedHouseholdName = "Test Household";

    protected override void ConfigureAppConfiguration(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-characters-long-for-hmac",
                ["Jwt:Issuer"] = "FinanceManager.Tests",
                ["Seed:OwnerEmail"] = SeedOwnerEmail,
                ["Seed:OwnerPassword"] = SeedOwnerPassword,
                ["Seed:OwnerName"] = SeedOwnerName,
                ["Seed:HouseholdName"] = SeedHouseholdName,
            });
        });
    }

    public async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        return body!.AccessToken;
    }

    public Task<string> LoginAsOwnerAsync(HttpClient client) =>
        LoginAsync(client, SeedOwnerEmail, SeedOwnerPassword);

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
