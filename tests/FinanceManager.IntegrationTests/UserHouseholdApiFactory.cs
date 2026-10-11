using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests;

public class UserHouseholdApiFactory : WebApplicationFactory<Program>
{
    public const string SeedOwnerEmail = "owner@test.local";
    public const string SeedOwnerPassword = "TestPassword123";
    public const string SeedOwnerName = "Test Owner";
    public const string SeedHouseholdName = "Test Household";

    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
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

        builder.ConfigureServices(services =>
        {
            // Same provider-swap pattern as FinancialAccountsApiFactory - see its comment
            // for why both DbContextOptions<T> and IDbContextOptionsConfiguration<T> must
            // be removed before re-adding the InMemory provider.
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<FinanceManagerDbContext>) ||
                    d.ServiceType == typeof(FinanceManagerDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(FinanceManagerDbContext)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<FinanceManagerDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
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
