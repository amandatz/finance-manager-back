using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Users;

public sealed class UsersService(IIdentityService identityService)
{
    public async Task<CurrentUserResponse?> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await identityService.GetUserAsync(userId, cancellationToken);

        return profile is null
            ? null
            : new CurrentUserResponse(profile.Id, profile.Name, profile.Email, profile.Role, profile.HouseholdId);
    }

    public Task<CreateUserResult> CreateMemberAsync(
        CreateMemberRequest request,
        Guid householdId,
        CancellationToken cancellationToken)
    {
        return identityService.CreateUserAsync(
            request.Name,
            request.Email,
            request.Password,
            householdId,
            HouseholdRole.Member,
            cancellationToken);
    }

    public Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        return identityService.ChangePasswordAsync(
            userId,
            request.CurrentPassword,
            request.NewPassword,
            cancellationToken);
    }
}
