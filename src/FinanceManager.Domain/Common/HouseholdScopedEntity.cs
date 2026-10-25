namespace FinanceManager.Domain.Common;

/// <summary>
/// Base class for every entity that belongs to exactly one <c>Household</c> (the tenant in
/// this system) - e.g. <c>FinancialAccount</c>. <see cref="HouseholdId"/> is set once, at
/// construction, by the derived type's constructor (never updated afterwards - an entity
/// never changes household).
///
/// Inheriting this class is all a new entity needs to do to opt into automatic multi-tenant
/// isolation: <see cref="FinanceManager.Infrastructure.Persistence.FinanceManagerDbContext"/>
/// reflects over every entity type implementing <see cref="IHouseholdScoped"/> and applies a
/// global EF Core query filter scoping every query to the current request's household. See
/// <c>back/docs/architecture-multitenancy.md</c> for the full mechanism.
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
