using FinanceManager.Domain.Common;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Entities;

/// <summary>
/// A household-wide spending/income category - no individual owner, unlike
/// <see cref="FinancialAccount"/>/<see cref="CreditCard"/>.
/// </summary>
public sealed class Category : HouseholdScopedEntity
{
    public const int NameMaxLength = 100;

    private Category()
    {
    }

    public Category(string name, CategoryType type, Guid householdId)
        : base(householdId)
    {
        ValidateName(name);
        ValidateType(type);

        Name = name;
        Type = type;
    }

    public string Name { get; private set; } = null!;

    public CategoryType Type { get; private set; }

    public void Update(string name, CategoryType type)
    {
        ValidateName(name);
        ValidateType(type);

        Name = name;
        Type = type;
        MarkAsUpdated();
    }

    private static void ValidateName(string name)
    {
        Guard.AgainstEmptyOrTooLong(name, NameMaxLength, nameof(name));
    }

    private static void ValidateType(CategoryType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainValidationException("Type is not a valid CategoryType.", nameof(type));
        }
    }
}
