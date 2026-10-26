using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Common;

/// <summary>
/// Base class for <see cref="HouseholdScopedEntity"/> entities with N:N individual owners within
/// that household, rather than a single <c>OwnerUserId</c> + <c>Visibility</c> flag.
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
    /// Every user who owns this entity - at least one, enforced below, with no duplicates.
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
