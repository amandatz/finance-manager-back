using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Identity;

public sealed class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<bool> AnyUsersExistAsync(CancellationToken cancellationToken)
    {
        return await userManager.Users.AnyAsync(cancellationToken);
    }

    public async Task<CreateUserResult> CreateUserAsync(
        string name,
        string email,
        string password,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return new CreateUserResult(CreateUserOutcome.EmailAlreadyInUse, null, []);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = name,
            HouseholdId = householdId,
            Role = role,
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            var isPasswordError = result.Errors.Any(
                e => e.Code.StartsWith("Password", StringComparison.Ordinal));

            return new CreateUserResult(
                isPasswordError ? CreateUserOutcome.PasswordPolicyViolation : CreateUserOutcome.EmailAlreadyInUse,
                null,
                errors);
        }

        return new CreateUserResult(CreateUserOutcome.Success, user.Id, []);
    }

    public async Task<AuthenticatedUser?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            return null;
        }

        return new AuthenticatedUser(user.Id, user.HouseholdId, user.Role);
    }

    public async Task<UserProfile?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null
            ? null
            : new UserProfile(user.Id, user.Name, user.Email!, user.Role, user.HouseholdId);
    }

    public async Task<ChangePasswordOutcome> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return ChangePasswordOutcome.CurrentPasswordIncorrect;
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (result.Succeeded)
        {
            return ChangePasswordOutcome.Success;
        }

        var isCurrentPasswordWrong = result.Errors.Any(e => e.Code == "PasswordMismatch");

        return isCurrentPasswordWrong
            ? ChangePasswordOutcome.CurrentPasswordIncorrect
            : ChangePasswordOutcome.NewPasswordPolicyViolation;
    }
}
