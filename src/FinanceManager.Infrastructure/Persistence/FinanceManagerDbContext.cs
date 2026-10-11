using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence;

public sealed class FinanceManagerDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    public FinanceManagerDbContext(
        DbContextOptions<FinanceManagerDbContext> options)
        : base(options)
    {
    }

    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

    public DbSet<Household> Households => Set<Household>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FinanceManagerDbContext).Assembly);
    }
}