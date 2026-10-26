namespace FinanceManager.Domain.Common;

/// <summary>
/// Base class for entities belonging to exactly one <c>Household</c> (the tenant). <see cref="HouseholdId"/>
/// is set once at construction and never changes. Inheriting this is all a new entity needs for automatic
/// multi-tenant isolation via <see cref="FinanceManager.Infrastructure.Persistence.FinanceManagerDbContext"/>'s global query filter.
/// </summary>
public abstract class HouseholdScopedEntity : Entity, IHouseholdScoped
{
    protected HouseholdScopedEntity(Guid householdId)
    {
        HouseholdId = householdId;
    }

    // Parameterless constructor for EF Core materialization only - mirrors the pattern used
    // by every other Entity subtype in this codebase (private/protected ctor for EF, public
    // ctor for real construction with validation).
    protected HouseholdScopedEntity()
    {
    }

    public Guid HouseholdId { get; protected set; }
}
