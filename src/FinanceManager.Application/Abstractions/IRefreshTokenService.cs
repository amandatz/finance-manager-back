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
    /// Redeems a refresh token for a new pair, rotating it in the process. Returns
    /// <c>null</c> for any rejection reason - unknown, expired, or an already-rotated
    /// token being replayed - the caller does not need to distinguish them.
    /// </summary>
    Task<TokenPair?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a refresh token. Idempotent: an unknown, already-revoked, or expired token
    /// is a no-op, never an error.
    /// </summary>
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken);
}

public sealed record TokenPair(string AccessToken, string RefreshToken);
