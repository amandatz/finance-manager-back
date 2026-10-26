using FinanceManager.Domain.Enums;

namespace FinanceManager.Infrastructure.Identity;

/// <summary>
/// A user's membership in one household, with their role in it. Infrastructure-only, not a Domain
/// <c>Entity</c>, since it ties directly to the Identity user.
/// </summary>
public sealed class HouseholdMembership
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid HouseholdId { get; set; }

    public HouseholdRole Role { get; set; }

    public DateTime CreatedAt { get; set; }
}
