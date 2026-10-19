using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Identity;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceManager.IntegrationTests;

/// <summary>
/// Simulates a valid, unexpired JWT whose subject no longer resolves to a user (e.g. the
/// account was deleted after the token was issued) - as opposed to
/// <see cref="ThrowingUserHouseholdApiFactory"/>, which simulates an unexpected failure.
/// This is an expected, named outcome (null, not an exception), so GetMe should map it to
/// a 401 ProblemDetails rather than a 500.
/// </summary>
public class UserGoneUserHouseholdApiFactory : UserHouseholdApiFactory
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.RemoveAll<IIdentityService>();
        services.AddScoped<IIdentityService>(provider =>
            new UserGoneOnGetUserIdentityService(
                new IdentityService(
                    provider.GetRequiredService<UserManager<ApplicationUser>>(),
                    provider.GetRequiredService<FinanceManagerDbContext>())));
    }

    private sealed class UserGoneOnGetUserIdentityService(IIdentityService inner) : IIdentityService
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
            Task.FromResult<UserProfile?>(null);

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
