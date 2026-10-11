using System.Diagnostics;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Application.Users;

public sealed class UsersService(IIdentityService identityService)
{
    public async Task<CurrentUserResponse> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await identityService.GetUserAsync(userId, cancellationToken);

        // The JWT was valid (authentication already succeeded upstream) but no longer
        // resolves to a user - e.g. the account was deleted after the token was issued.
        // Treated as an authentication failure, not a 404: the caller's credential is
        // what's no longer valid, not a resource they asked for by id.
        if (profile is null)
        {
            throw new AuthenticationFailedException("The current user could not be found.");
        }

        return new CurrentUserResponse(profile.Id, profile.Name, profile.Email, profile.Role, profile.HouseholdId);
    }

    public async Task<CreatedUserResponse> CreateMemberAsync(
        CreateMemberRequest request,
        Guid householdId,
        CancellationToken cancellationToken)
    {
        var result = await identityService.CreateUserAsync(
            request.Name,
            request.Email,
            request.Password,
            householdId,
            HouseholdRole.Member,
            cancellationToken);

        switch (result.Outcome)
        {
            case CreateUserOutcome.Success:
                return new CreatedUserResponse(result.UserId!.Value, request.Name, request.Email);
            case CreateUserOutcome.EmailAlreadyInUse:
                throw new ConflictException("Email already in use.");
            case CreateUserOutcome.PasswordPolicyViolation:
                throw new BusinessRuleViolationException(
                    "The password does not meet the password policy.",
                    result.Errors);
            default:
                throw new UnreachableException($"Unhandled {nameof(CreateUserOutcome)}: {result.Outcome}");
        }
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var outcome = await identityService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);

        switch (outcome)
        {
            case ChangePasswordOutcome.Success:
                return;
            case ChangePasswordOutcome.CurrentPasswordIncorrect:
                throw new BusinessRuleViolationException("The current password is incorrect.");
            case ChangePasswordOutcome.NewPasswordPolicyViolation:
                throw new BusinessRuleViolationException("The new password does not meet the password policy.");
            default:
                throw new UnreachableException($"Unhandled {nameof(ChangePasswordOutcome)}: {outcome}");
        }
    }
}
