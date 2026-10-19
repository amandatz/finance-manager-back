using FinanceManager.Domain.Enums;

namespace FinanceManager.Infrastructure.Identity;

/// <summary>
/// A user's membership in one household, with their role in it. Infrastructure-only, not a
/// Domain <c>Entity</c> - same reasoning as <see cref="ApplicationUser"/> and
/// <see cref="RefreshToken"/>: it ties directly to the Identity user, which Domain does not know.
/// </summary>
public sealed class HouseholdMembership
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid HouseholdId { get; set; }

    public HouseholdRole Role { get; set; }

    public DateTime CreatedAt { get; set; }
}
