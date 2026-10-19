using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Abstractions;

public interface IIdentityService
{
    Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken);

    Task<CreateUserResult> CreateUserAsync(
        string name,
        string email,
        string password,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken);

    Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<UserProfile?> GetUserAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);

    Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<HouseholdMembershipInfo>> GetMembershipsAsync(Guid userId, CancellationToken cancellationToken);

    Task<HouseholdRole?> GetMembershipRoleAsync(Guid userId, Guid householdId, CancellationToken cancellationToken);

    Task<AddMembershipResult> AddMembershipAsync(
        Guid userId,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken);
}
