using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;

namespace FinanceManager.UnitTests.Users;

/// <summary>
/// A fake <see cref="IIdentityService"/> whose result for each method is set directly by
/// the test, so <see cref="FinanceManager.Application.Users.UsersService"/>'s translation
/// of identity outcomes into domain exceptions can be unit-tested without EF Core/
/// ASP.NET Identity.
/// </summary>
internal sealed class ConfigurableFakeIdentityService : IIdentityService
{
    public CreateUserResult CreateUserResult { get; set; } = new(CreateUserOutcome.Success, Guid.NewGuid(), []);

    public ChangePasswordOutcome ChangePasswordOutcome { get; set; } = ChangePasswordOutcome.Success;

    public UserProfile? UserProfile { get; set; }

    public IReadOnlyList<HouseholdMembershipInfo> Memberships { get; set; } = [];

    public Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<CreateUserResult> CreateUserAsync(
        string name,
        string email,
        string password,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken) =>
        Task.FromResult(CreateUserResult);

    public Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email, string password, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<UserProfile?> GetUserAsync(Guid userId, Guid householdId, CancellationToken cancellationToken) =>
        Task.FromResult(UserProfile);

    public Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken) =>
        Task.FromResult(ChangePasswordOutcome);

    public Task<IReadOnlyList<HouseholdMembershipInfo>> GetMembershipsAsync(
        Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Memberships);

    public Task<HouseholdRole?> GetMembershipRoleAsync(
        Guid userId, Guid householdId, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<AddMembershipResult> AddMembershipAsync(
        Guid userId, Guid householdId, HouseholdRole role, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
