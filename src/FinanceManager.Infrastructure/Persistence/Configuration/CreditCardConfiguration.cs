using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

internal sealed class CreditCardConfiguration : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Brand)
            .HasMaxLength(CreditCard.BrandMaxLength);

        builder.Property(x => x.CreditLimit)
            .HasColumnType("decimal(18,2)");

        builder.ConfigureHouseholdScopedOwnedEntity();
    }
}
