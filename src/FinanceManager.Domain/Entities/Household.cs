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
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (name.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Name must not exceed {NameMaxLength} characters.",
                nameof(name));
        }
    }
}
