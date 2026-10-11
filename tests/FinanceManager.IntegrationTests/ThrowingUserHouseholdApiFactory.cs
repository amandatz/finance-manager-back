using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Identity;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceManager.IntegrationTests;

public class ThrowingUserHouseholdApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-characters-long-for-hmac",
                ["Jwt:Issuer"] = "FinanceManager.Tests",
                ["Seed:OwnerEmail"] = UserHouseholdApiFactory.SeedOwnerEmail,
                ["Seed:OwnerPassword"] = UserHouseholdApiFactory.SeedOwnerPassword,
                ["Seed:OwnerName"] = UserHouseholdApiFactory.SeedOwnerName,
                ["Seed:HouseholdName"] = UserHouseholdApiFactory.SeedHouseholdName,
            });
        });

        builder.ConfigureServices(services =>
        {
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

            // Seeding and login must still work, so only GetUserAsync (the /me endpoint)
            // throws - everything else delegates to the real IdentityService.
            services.RemoveAll<IIdentityService>();
            services.AddScoped<IIdentityService>(provider =>
                new ThrowingOnGetUserIdentityService(
                    new IdentityService(provider.GetRequiredService<UserManager<ApplicationUser>>())));
        });
    }

    public async Task<string> LoginAsOwnerAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(UserHouseholdApiFactory.SeedOwnerEmail, UserHouseholdApiFactory.SeedOwnerPassword));

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(TestJsonOptions.Default);
        return body!.AccessToken;
    }

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private sealed class ThrowingOnGetUserIdentityService(IIdentityService inner) : IIdentityService
    {
        public Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken) =>
            inner.AnyUsersExistAsync(cancellationToken);

        public Task<CreateUserResult> CreateUserAsync(
            string name,
            string email,
            string password,
            Guid householdId,
            HouseholdRole role,
            CancellationToken cancellationToken) =>
            inner.CreateUserAsync(name, email, password, householdId, role, cancellationToken);

        public Task<AuthenticatedUser?> ValidateCredentialsAsync(
            string email, string password, CancellationToken cancellationToken) =>
            inner.ValidateCredentialsAsync(email, password, cancellationToken);

        public Task<UserProfile?> GetUserAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure.");

        public Task<ChangePasswordOutcome> ChangePasswordAsync(
            Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken) =>
            inner.ChangePasswordAsync(userId, currentPassword, newPassword, cancellationToken);
    }
}
