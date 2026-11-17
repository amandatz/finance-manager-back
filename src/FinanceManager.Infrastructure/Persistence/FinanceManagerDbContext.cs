using System.Linq.Expressions;
using System.Reflection;
using FinanceManager.Application.Abstractions;
using FinanceManager.Domain.Common;
using FinanceManager.Domain.Entities;
using FinanceManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceManager.Infrastructure.Persistence;

public sealed class FinanceManagerDbContext : IdentityUserContext<ApplicationUser, Guid>
{
    private readonly ICurrentHouseholdAccessor _currentHouseholdAccessor;

    public FinanceManagerDbContext(
        DbContextOptions<FinanceManagerDbContext> options,
        ICurrentHouseholdAccessor currentHouseholdAccessor)
        : base(options)
    {
        _currentHouseholdAccessor = currentHouseholdAccessor;
    }

    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();

    public DbSet<CreditCard> CreditCards => Set<CreditCard>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Household> Households => Set<Household>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<HouseholdMembership> HouseholdMemberships => Set<HouseholdMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Same reasoning as ApplicationUserConfiguration.ToTable("asp_net_users") - these
        // framework-owned entity types have no IEntityTypeConfiguration<T> of their own to
        // put the override in, so it lives here instead.
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("asp_net_user_claims");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("asp_net_user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("asp_net_user_tokens");

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FinanceManagerDbContext).Assembly);

        ApplyHouseholdScopedQueryFilters(modelBuilder);
    }

    /// <summary>
    /// Applies a global query filter scoping every <see cref="IHouseholdScoped"/> entity to the current household.
    /// Captures <c>this</c> rather than a materialized id, so it re-evaluates <see cref="ICurrentHouseholdAccessor.HouseholdId"/> on every query.
    /// </summary>
    private void ApplyHouseholdScopedQueryFilters(ModelBuilder modelBuilder)
    {
        var applyFilterMethod = typeof(FinanceManagerDbContext)
            .GetMethod(nameof(ApplyHouseholdScopedQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (!typeof(IHouseholdScoped).IsAssignableFrom(clrType))
            {
                continue;
            }

            applyFilterMethod
                .MakeGenericMethod(clrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void ApplyHouseholdScopedQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IHouseholdScoped
    {
        Expression<Func<TEntity, bool>> filter = entity =>
            entity.HouseholdId == _currentHouseholdAccessor.HouseholdId;

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}