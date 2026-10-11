using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace FinanceManager.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id = Guid.NewGuid();
    }

    public string Name { get; set; } = null!;

    public Guid HouseholdId { get; set; }

    public HouseholdRole Role { get; set; }
}
