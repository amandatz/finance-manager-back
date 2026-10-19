using FinanceManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

internal sealed class HouseholdMembershipConfiguration : IEntityTypeConfiguration<HouseholdMembership>
{
    public void Configure(EntityTypeBuilder<HouseholdMembership> builder)
    {
        builder.ToTable("HouseholdMemberships");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.UserId, x.HouseholdId })
            .IsUnique();
    }
}
