using FinanceManager.Application.Users;
using FinanceManager.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinanceManager.Infrastructure.Persistence.Configuration;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // ASP.NET Identity's own base OnModelCreating names this table "AspNetUsers"
        // explicitly, which the snake_case naming convention leaves alone as a developer
        // override - so it needs an explicit override of its own to be renamed.
        builder.ToTable("asp_net_users");

        // Same reasoning as the ToTable override above - Identity names these "EmailIndex"
        // and "UserNameIndex" explicitly, which the naming convention leaves alone.
        builder.HasIndex(x => x.NormalizedEmail).HasDatabaseName("ix_asp_net_users_normalized_email");
        builder.HasIndex(x => x.NormalizedUserName).IsUnique().HasDatabaseName("ix_asp_net_users_normalized_user_name");

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(UserConstraints.NameMaxLength);
    }
}
