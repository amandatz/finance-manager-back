using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Identity;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceManager.IntegrationTests;

public class ThrowingUserHouseholdApiFactory : UserHouseholdApiFactory
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Registration and login must still work, so only GetUserAsync (the /me endpoint)
        // throws - everything else delegates to the real IdentityService.
        services.RemoveAll<IIdentityService>();
        services.AddScoped<IIdentityService>(provider =>
            new ThrowingOnGetUserIdentityService(
                new IdentityService(
                    provider.GetRequiredService<UserManager<ApplicationUser>>(),
                    provider.GetRequiredService<FinanceManagerDbContext>())));
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

        public Task<UserProfile?> GetUserAsync(Guid userId, Guid householdId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Simulated failure.");

        public Task<ChangePasswordOutcome> ChangePasswordAsync(
            Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken) =>
            inner.ChangePasswordAsync(userId, currentPassword, newPassword, cancellationToken);

        public Task<IReadOnlyList<HouseholdMembershipInfo>> GetMembershipsAsync(
            Guid userId, CancellationToken cancellationToken) =>
            inner.GetMembershipsAsync(userId, cancellationToken);

        public Task<HouseholdRole?> GetMembershipRoleAsync(
            Guid userId, Guid householdId, CancellationToken cancellationToken) =>
            inner.GetMembershipRoleAsync(userId, householdId, cancellationToken);

        public Task<AddMembershipResult> AddMembershipAsync(
            Guid userId, Guid householdId, HouseholdRole role, CancellationToken cancellationToken) =>
            inner.AddMembershipAsync(userId, householdId, role, cancellationToken);
    }
}
