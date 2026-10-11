using FinanceManager.Application.Users;
using FinanceManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(UserConstraints.NameMaxLength);

        builder.Property(x => x.HouseholdId)
            .IsRequired();
    }
}
