using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FinanceManager.IntegrationTests;

public class FinancialAccountsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // AddDbContext composes every registered IDbContextOptionsConfiguration<T>
            // rather than replacing it, so the real UseNpgsql(...) call from
            // AddInfrastructure() must be removed explicitly - removing only
            // DbContextOptions<FinanceManagerDbContext> leaves it in place and both
            // providers end up configured on the same options, which EF rejects.
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<FinanceManagerDbContext>) ||
                    d.ServiceType == typeof(FinanceManagerDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>) &&
                     d.ServiceType.GenericTypeArguments[0] == typeof(FinanceManagerDbContext)))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<FinanceManagerDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

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
