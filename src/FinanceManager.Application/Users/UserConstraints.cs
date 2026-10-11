namespace FinanceManager.Application.Users;

/// <summary>
/// Shared constraints on user data - referenced by both request validators and the EF
/// configuration for <c>ApplicationUser</c>, so the two stay in sync from one source.
/// </summary>
public static class UserConstraints
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 256;
}
