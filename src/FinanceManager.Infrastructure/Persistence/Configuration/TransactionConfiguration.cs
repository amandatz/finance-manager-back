using FinanceManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Description)
            .HasMaxLength(Transaction.DescriptionMaxLength);

        builder.Property(x => x.HouseholdId)
            .IsRequired();

        builder.HasOne<FinancialAccount>()
            .WithMany()
            .HasForeignKey(x => x.FinancialAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CreditCard>()
            .WithMany()
            .HasForeignKey(x => x.CreditCardId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.HouseholdId);
        builder.HasIndex(x => x.FinancialAccountId);
        builder.HasIndex(x => x.CreditCardId);
        builder.HasIndex(x => x.CategoryId);
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => x.SettlementGroupId);
    }
}
