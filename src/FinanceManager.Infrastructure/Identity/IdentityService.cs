using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Enums;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Identity;

public sealed class IdentityService(
    UserManager<ApplicationUser> userManager,
    FinanceManagerDbContext dbContext) : IIdentityService
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

        dbContext.HouseholdMemberships.Add(new HouseholdMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            HouseholdId = householdId,
            Role = role,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

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

        // A user with more than one membership activates the earliest-created one by
        // default - see .tasks/household-self-registration.md Unresolved #1.
        var membership = await dbContext.HouseholdMemberships
            .Where(m => m.UserId == user.Id)
            .OrderBy(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (membership is null)
        {
            // Defensive: every user is created with at least one membership. Treated the
            // same as invalid credentials rather than a distinct error.
            return null;
        }

        return new AuthenticatedUser(user.Id, membership.HouseholdId, membership.Role);
    }

    public async Task<UserProfile?> GetUserAsync(Guid userId, Guid householdId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var membership = await dbContext.HouseholdMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.HouseholdId == householdId, cancellationToken);

        return membership is null
            ? null
            : new UserProfile(user.Id, user.Name, user.Email!, membership.Role, householdId);
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

    public async Task<IReadOnlyList<HouseholdMembershipInfo>> GetMembershipsAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.HouseholdMemberships
            .Where(m => m.UserId == userId)
            .Join(
                dbContext.Households,
                m => m.HouseholdId,
                h => h.Id,
                (m, h) => new HouseholdMembershipInfo(h.Id, h.Name, m.Role, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<HouseholdRole?> GetMembershipRoleAsync(
        Guid userId, Guid householdId, CancellationToken cancellationToken)
    {
        var membership = await dbContext.HouseholdMemberships
            .FirstOrDefaultAsync(m => m.UserId == userId && m.HouseholdId == householdId, cancellationToken);

        return membership?.Role;
    }

    public async Task<AddMembershipResult> AddMembershipAsync(
        Guid userId,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.HouseholdMemberships
            .AnyAsync(m => m.UserId == userId && m.HouseholdId == householdId, cancellationToken);

        if (existing)
        {
            return new AddMembershipResult(AddMembershipOutcome.AlreadyMember);
        }

        dbContext.HouseholdMemberships.Add(new HouseholdMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            HouseholdId = householdId,
            Role = role,
            CreatedAt = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AddMembershipResult(AddMembershipOutcome.Success);
    }
}
