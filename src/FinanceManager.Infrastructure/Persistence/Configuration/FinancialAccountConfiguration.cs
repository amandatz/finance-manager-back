using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

internal sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(FinancialAccount.NameMaxLength);

        builder.Property(x => x.InstitutionName)
            .HasMaxLength(FinancialAccount.InstitutionNameMaxLength);

        builder.ConfigureHouseholdScopedOwnedEntity();
    }
}
