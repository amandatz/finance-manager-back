namespace FinanceManager.Domain.Common;

/// <summary>
/// Marker interface for entities that belong to exactly one <c>Household</c> (the tenant in
/// this system) and must therefore be isolated per-household at the persistence layer.
///
/// Implemented by <see cref="HouseholdScopedEntity"/> so every concrete entity that inherits
/// from it gets isolation "for free" - but it is a separate interface (rather than only
/// relying on "is assignable to HouseholdScopedEntity") because:
/// - EF Core's <c>HasQueryFilter</c> wiring in <c>FinanceManagerDbContext.OnModelCreating</c>
///   reflects over <c>modelBuilder.Model.GetEntityTypes()</c> and needs a simple, uniform way
///   to ask "does this CLR type carry a HouseholdId that must be filtered on" - checking
///   interface assignability is exactly as easy as checking base-class assignability, but the
///   interface also works for any future entity that, for whatever reason, cannot inherit
///   <see cref="HouseholdScopedEntity"/> (e.g. it needs a different base class already).
/// - The interface documents the contract independently of the inheritance hierarchy: "any
///   type implementing IHouseholdScoped must be tenant-isolated" is the actual invariant the
///   global query filter enforces, not "any type inheriting HouseholdScopedEntity".
/// </summary>
public interface IHouseholdScoped
{
    Guid HouseholdId { get; }
}
