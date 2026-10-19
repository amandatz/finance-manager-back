using System.Diagnostics;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;
using FinanceManager.Domain.Repositories;

namespace FinanceManager.Application.Users;

public sealed class AuthService(
    IIdentityService identityService,
    IHouseholdRepository householdRepository,
    IRefreshTokenService refreshTokenService)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var authenticated = await identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (authenticated is null)
        {
            // Deliberately generic: never reveal whether the email exists or the
            // password was wrong.
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var tokens = await refreshTokenService.IssueTokensAsync(
            authenticated.UserId,
            authenticated.HouseholdId,
            authenticated.Role,
            cancellationToken);

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokens = await refreshTokenService.RefreshAsync(request.RefreshToken, cancellationToken);

        if (tokens is null)
        {
            throw new AuthenticationFailedException("Invalid or expired refresh token.");
        }

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken);
    }

    public Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken) =>
        refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);

    public async Task<LoginResponse> SwitchHouseholdAsync(
        Guid userId, Guid targetHouseholdId, CancellationToken cancellationToken)
    {
        var role = await identityService.GetMembershipRoleAsync(userId, targetHouseholdId, cancellationToken)
            ?? throw NotFoundException.For("HouseholdMembership", targetHouseholdId);

        var tokens = await refreshTokenService.IssueTokensAsync(userId, targetHouseholdId, role, cancellationToken);

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken);
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        Household household;
        HouseholdRole role;

        if (string.IsNullOrWhiteSpace(request.InviteCode))
        {
            household = new Household(request.HouseholdName!);
            await householdRepository.AddAsync(household, cancellationToken);
            await householdRepository.SaveChangesAsync(cancellationToken);
            role = HouseholdRole.Owner;
        }
        else
        {
            household = await householdRepository.GetByInviteCodeAsync(request.InviteCode, cancellationToken)
                ?? throw new BusinessRuleViolationException(
                    "The invite code is invalid or has already been used.");
            role = HouseholdRole.Member;
        }

        var result = await identityService.CreateUserAsync(
            request.Name,
            request.Email,
            request.Password,
            household.Id,
            role,
            cancellationToken);

        switch (result.Outcome)
        {
            case CreateUserOutcome.Success:
                break;
            case CreateUserOutcome.EmailAlreadyInUse:
                throw new ConflictException("Email already in use.");
            case CreateUserOutcome.PasswordPolicyViolation:
                throw new BusinessRuleViolationException(
                    "The password does not meet the password policy.",
                    result.Errors);
            default:
                throw new UnreachableException($"Unhandled {nameof(CreateUserOutcome)}: {result.Outcome}");
        }

        if (!string.IsNullOrWhiteSpace(request.InviteCode))
        {
            // Consume only after the user was actually created, so a failure above (e.g.
            // duplicate email) never burns a valid code for an unrelated reason.
            household.ConsumeInviteCode();
            await householdRepository.SaveChangesAsync(cancellationToken);
        }

        var tokens = await refreshTokenService.IssueTokensAsync(
            result.UserId!.Value,
            household.Id,
            role,
            cancellationToken);

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken);
    }
}
