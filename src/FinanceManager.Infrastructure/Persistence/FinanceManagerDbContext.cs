using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence;

public sealed class FinanceManagerDbContext : DbContext
{
    public FinanceManagerDbContext(
        DbContextOptions<FinanceManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FinanceManagerDbContext).Assembly);
    }
}