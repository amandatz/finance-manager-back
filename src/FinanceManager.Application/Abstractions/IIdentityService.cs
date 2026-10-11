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

    Task<UserProfile?> GetUserAsync(Guid userId, CancellationToken cancellationToken);

    Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
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

public enum ChangePasswordOutcome
{
    Success,
    CurrentPasswordIncorrect,
    NewPasswordPolicyViolation,
}
