namespace FinanceManager.Application.Abstractions;

/// <summary>
/// Resolves the "current household" (tenant) for the in-flight request, from the
/// authenticated user's JWT <c>household_id</c> claim - the same claim already minted by
/// <c>JwtTokenGenerator</c> on login/register/refresh/switch-household. Consumed by
/// <c>FinanceManagerDbContext</c>'s global query filter so every query against an
/// <c>IHouseholdScoped</c> entity is automatically scoped to this household, with no manual
/// <c>.Where(householdId...)</c> needed in repositories/services.
/// </summary>
public interface ICurrentHouseholdAccessor
{
    /// <summary>
    /// The current request's household id.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// There is no authenticated household context (missing/unparseable claim). Should never
    /// happen in practice, since every household-scoped endpoint requires <c>[Authorize]</c>.
    /// </exception>
    Guid HouseholdId { get; }
}
