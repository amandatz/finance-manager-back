using FinanceManager.Domain.Enums;

namespace FinanceManager.Application.Abstractions;

public interface IRefreshTokenService
{
    Task<TokenPair> IssueTokensAsync(
        Guid userId,
        Guid householdId,
        HouseholdRole role,
        CancellationToken cancellationToken);

    /// <summary>
    /// Redeems and rotates a refresh token. Returns <c>null</c> for any rejection reason; callers don't need to distinguish them.
    /// </summary>
    Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a refresh token. Idempotent - an unknown or already-revoked token is a no-op, never an error.
    /// </summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}
