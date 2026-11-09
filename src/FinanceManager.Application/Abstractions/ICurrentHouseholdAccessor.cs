namespace FinanceManager.Application.Abstractions;

/// <summary>
/// Resolves the current request's household (tenant) from the authenticated user's JWT claim.
/// </summary>
public interface ICurrentHouseholdAccessor
{
    /// <summary>
    /// The current request's household id.
    /// </summary>
    /// <exception cref="FinanceManager.Domain.Exceptions.AuthenticationFailedException">No authenticated household context is available.</exception>
    Guid HouseholdId { get; }
}
