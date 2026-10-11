using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceManager.IntegrationTests;

public class ThrowingUserHouseholdApiFactory : UserHouseholdApiFactory
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Seeding and login must still work, so only GetUserAsync (the /me endpoint)
        // throws - everything else delegates to the real IdentityService.
        services.RemoveAll<IIdentityService>();
        services.AddScoped<IIdentityService>(provider =>
            new ThrowingOnGetUserIdentityService(
                new IdentityService(provider.GetRequiredService<UserManager<ApplicationUser>>())));
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
