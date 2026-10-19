using FinanceManager.Domain.Enums;

namespace FinanceManager.Infrastructure.Identity;

/// <summary>
/// A persisted, revocable refresh token. Not a Domain <c>Entity</c> - this is a session/
/// security record, not a business concept, so it stays Infrastructure-only.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>
    /// Snapshot of which household/role this session is scoped to, taken when the token was
    /// issued (login, switch, or join). Rotation carries it forward unchanged - refresh never
    /// re-derives "current" household/role from live membership state.
    /// </summary>
    public Guid HouseholdId { get; set; }

    public HouseholdRole Role { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Concurrency token: an EF-generated UPDATE for this row includes
    /// <c>WHERE RevokedAt = &lt;original value&gt;</c>, so claiming an active token for
    /// rotation is atomic - a concurrent claim of the same token throws
    /// <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> instead of
    /// silently succeeding twice.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
