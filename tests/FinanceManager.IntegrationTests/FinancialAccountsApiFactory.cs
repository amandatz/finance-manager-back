using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests;

/// <summary>
/// Combines household/auth test helpers (<see cref="UserHouseholdApiFactory"/>) with
/// FinancialAccount seeding helpers, since every financial-account endpoint is now
/// household-scoped and <c>[Authorize]</c> - tests need both an authenticated caller and a
/// way to seed accounts directly into a specific household, bypassing the global query
/// filter (seeding always goes straight through the DbContext, never through the filtered
/// repository).
/// </summary>
public class FinancialAccountsApiFactory : UserHouseholdApiFactory
{
    public async Task<FinancialAccount> SeedAccountAsync(FinancialAccount account)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        db.FinancialAccounts.Add(account);
        await db.SaveChangesAsync();

        return account;
    }

    public async Task<FinancialAccount?> FindAccountAsync(Guid id)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceManagerDbContext>();

        // IgnoreQueryFilters: this helper is used by tests asserting on raw persisted state
        // (e.g. "was it soft-deleted") regardless of which household is "current" for the
        // (nonexistent, in this helper) request - using the filtered DbSet would make the
        // lookup spuriously fail since there is no ICurrentHouseholdAccessor context here.
        return await db.FinancialAccounts.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id);
    }
}
