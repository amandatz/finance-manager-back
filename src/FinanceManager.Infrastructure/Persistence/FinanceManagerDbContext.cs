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
    /// Walks every entity type in the model and, for each one assignable to
    /// <see cref="IHouseholdScoped"/>, applies a global query filter restricting rows to the
    /// current request's household (<see cref="_currentHouseholdAccessor"/>). This is the
    /// entire multi-tenant isolation mechanism: a new entity opts in purely by implementing
    /// <see cref="IHouseholdScoped"/> (in practice, by inheriting
    /// <see cref="HouseholdScopedEntity"/>) - nothing here is aware of individual entity
    /// types by name, so task 007's <c>Transaction</c>/<c>Category</c> get isolation for free.
    ///
    /// The filter captures <c>this</c> (the DbContext instance, which holds the accessor) in
    /// the expression tree rather than a materialized Guid, so it re-evaluates
    /// <see cref="ICurrentHouseholdAccessor.HouseholdId"/> on every query - correct because a
    /// new DbContext instance (and hence a fresh read of the claim) is created per request.
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