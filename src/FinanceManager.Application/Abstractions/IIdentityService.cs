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

public enum CreateUserOutcome
{
    Success,
    EmailAlreadyInUse,
    PasswordPolicyViolation,
}

public sealed record CreateUserResult(
    CreateUserOutcome Outcome,
    Guid? UserId,
    IReadOnlyList<string> Errors);

public sealed record AuthenticatedUser(Guid UserId, Guid HouseholdId, HouseholdRole Role);

public sealed record UserProfile(Guid Id, string Name, string Email, HouseholdRole Role, Guid HouseholdId);

public sealed record HouseholdMembershipInfo(Guid HouseholdId, string HouseholdName, HouseholdRole Role, DateTime CreatedAt);

public enum ChangePasswordOutcome
{
    Success,
    CurrentPasswordIncorrect,
    NewPasswordPolicyViolation,
}

public enum AddMembershipOutcome
{
    Success,
    AlreadyMember,
}

public sealed record AddMembershipResult(AddMembershipOutcome Outcome);
