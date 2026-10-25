using FinanceManager.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

/// <summary>
/// Shared EF Core mapping for every <see cref="HouseholdScopedOwnedEntity"/> subtype
/// (<c>FinancialAccount</c>, <c>CreditCard</c>) - the <c>HouseholdId</c> property and the
/// owners collection are identical across all of them, so each
/// <see cref="IEntityTypeConfiguration{TEntity}"/> calls this once instead of repeating the
/// mapping.
/// </summary>
internal static class HouseholdScopedOwnedEntityConfigurationExtensions
{
    public static void ConfigureHouseholdScopedOwnedEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : HouseholdScopedOwnedEntity
    {
        builder.Property(x => x.HouseholdId)
            .IsRequired();

        builder.HasIndex(x => x.HouseholdId);

        // Owners are a N:N-shaped concept (which users own this entity), but modeled as part
        // of the owning aggregate (not a standalone Domain entity/repository) since owners
        // are intrinsic to the entity, not an identity/claims concern the way
        // HouseholdMembership is. Persisted as a primitive collection column (Postgres
        // uuid[], backed by the private List<Guid> field) rather than a join table - owners
        // are never queried independently of their entity, so a dedicated table would add
        // schema without a corresponding access pattern. The domain enforces "at least one
        // owner, no duplicates" (see HouseholdScopedOwnedEntity.AddOwner/RemoveOwner), not
        // the schema.
        builder.PrimitiveCollection<List<Guid>>("_ownerUserIds")
            .HasColumnName("owner_user_ids");
    }
}
