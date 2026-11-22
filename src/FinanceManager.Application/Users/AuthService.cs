using System.Diagnostics;
using FinanceManager.Application.Abstractions;
using FinanceManager.Application.Users.Requests;
using FinanceManager.Application.Users.Responses;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Application.Users;

public sealed class AuthService(
    IIdentityService identityService,
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

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, authenticated.HouseholdId);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokens = await refreshTokenService.RefreshAsync(request.RefreshToken, cancellationToken);

        if (tokens is null)
        {
            throw new AuthenticationFailedException("Invalid or expired refresh token.");
        }

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.HouseholdId);
    }

    public Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken) =>
        refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);

    public async Task<LoginResponse> SwitchHouseholdAsync(
        Guid userId, Guid targetHouseholdId, CancellationToken cancellationToken)
    {
        var role = await identityService.GetMembershipRoleAsync(userId, targetHouseholdId, cancellationToken)
            ?? throw NotFoundException.For("HouseholdMembership", targetHouseholdId);

        var tokens = await refreshTokenService.IssueTokensAsync(userId, targetHouseholdId, role, cancellationToken);

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.HouseholdId);
    }

    public async Task<LoginResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await identityService.CreateUserAsync(
            request.Name,
            request.Email,
            request.Password,
            householdId: null,
            role: null,
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

        var tokens = await refreshTokenService.IssueTokensAsync(
            result.UserId!.Value,
            householdId: null,
            role: null,
            cancellationToken);

        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, tokens.HouseholdId);
    }
}
