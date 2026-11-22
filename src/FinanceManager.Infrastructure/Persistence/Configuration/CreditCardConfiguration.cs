using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
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

        builder.Property(x => x.Color)
            .IsRequired()
            .HasDefaultValue(CreditCardColor.Graphite);

        builder.ConfigureHouseholdScopedOwnedEntity();
    }
}
