using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Common;

/// <summary>
/// Base class for entities that are both household-scoped (see
/// <see cref="HouseholdScopedEntity"/>) and have one or more individual owners within that
/// household - e.g. <c>FinancialAccount</c> and <c>CreditCard</c> today. Ownership is N:N
/// (an entity can have 2+ owners, meaning "shared" between exactly those people) rather than
/// a single <c>OwnerUserId</c> + <c>Visibility</c> flag: see
/// <c>back/docs/architecture-multitenancy.md</c> for the full rationale.
///
/// Shared here (rather than duplicated per entity) because both current consumers need the
/// identical invariants: at least one owner at all times, no duplicate owners, and the same
/// "only an owner can mutate/delete" authorization shape at the application layer.
/// </summary>
public abstract class HouseholdScopedOwnedEntity : HouseholdScopedEntity
{
    private readonly List<Guid> _ownerUserIds = [];

    protected HouseholdScopedOwnedEntity(Guid householdId, Guid ownerUserId)
        : base(householdId)
    {
        _ownerUserIds.Add(ownerUserId);
    }

    // Parameterless constructor for EF Core materialization only.
    protected HouseholdScopedOwnedEntity()
    {
    }

    /// <summary>
    /// Every user who owns this entity - at least one (enforced below), with no duplicates.
    /// An entity with exactly one owner behaves like what used to be called "Personal" in
    /// practice; 2+ owners means shared between exactly those people.
    /// </summary>
    public IReadOnlyCollection<Guid> OwnerUserIds => _ownerUserIds.AsReadOnly();

    public bool IsOwnedBy(Guid userId) => _ownerUserIds.Contains(userId);

    public void AddOwner(Guid userId)
    {
        if (_ownerUserIds.Contains(userId))
        {
            return;
        }

        _ownerUserIds.Add(userId);
        MarkAsUpdated();
    }

    public void RemoveOwner(Guid userId)
    {
        if (_ownerUserIds.Count <= 1)
        {
            throw new BusinessRuleViolationException("An entity must have at least one owner.");
        }

        _ownerUserIds.Remove(userId);
        MarkAsUpdated();
    }
}
