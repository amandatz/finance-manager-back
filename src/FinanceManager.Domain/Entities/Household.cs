using FinanceManager.Domain.Common;

namespace FinanceManager.Domain.Entities;

public sealed class Household : Entity
{
    public const int NameMaxLength = 100;

    private Household()
    {
    }

    public Household(string name)
    {
        ValidateName(name);

        Name = name;
    }

    public string Name { get; private set; } = null!;

    private static void ValidateName(string name)
    {
        Guard.AgainstEmptyOrTooLong(name, NameMaxLength, nameof(name));
    }
}
