using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(Category.NameMaxLength);

        builder.Property(x => x.HouseholdId)
            .IsRequired();

        builder.Property(x => x.DefaultIsHouseholdExpense)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(x => x.HouseholdId);
    }
}
