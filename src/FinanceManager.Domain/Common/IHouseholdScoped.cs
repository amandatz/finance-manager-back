namespace FinanceManager.Domain.Common;

/// <summary>
/// Marker interface for entities isolated per-household at the persistence layer. Implemented by
/// <see cref="HouseholdScopedEntity"/>, kept separate so the query filter can target the contract
/// directly rather than relying on base-class assignability.
/// </summary>
public interface IHouseholdScoped
{
    Guid HouseholdId { get; }
}
