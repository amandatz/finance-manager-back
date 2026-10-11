using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests;

public class FinancialAccountsApiFactory : InMemoryDbApiFactory
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

        return await db.FinancialAccounts.FindAsync(id);
    }
}
