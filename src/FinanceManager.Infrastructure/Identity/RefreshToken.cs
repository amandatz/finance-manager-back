using FinanceManager.Domain.Enums;

namespace FinanceManager.Infrastructure.Identity;

/// <summary>
/// A persisted, revocable refresh token. Not a Domain <c>Entity</c> - a session/security record, not a business concept.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Snapshot of the household/role this session was issued for. Rotation carries it forward
    /// unchanged; it never re-derives from live membership state. Null when the session has no
    /// active household yet.
    /// </summary>
    public Guid? HouseholdId { get; set; }

    public HouseholdRole? Role { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Also doubles as a concurrency token, so a concurrent rotation of the same token throws
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> instead of succeeding twice.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
