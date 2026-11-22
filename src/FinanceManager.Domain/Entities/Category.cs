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

    public Category(string name, CategoryType type, Guid householdId, bool defaultIsHouseholdExpense = false)
        : base(householdId)
    {
        ValidateName(name);
        ValidateType(type);

        Name = name;
        Type = type;
        DefaultIsHouseholdExpense = defaultIsHouseholdExpense;
    }

    public string Name { get; private set; } = null!;

    public CategoryType Type { get; private set; }

    /// <summary>
    /// Only meaningful for <see cref="CategoryType.Expense"/> categories - income has no
    /// household/personal distinction. Transactions that don't set <c>IsHouseholdExpense</c>
    /// explicitly inherit this value from their category.
    /// </summary>
    public bool DefaultIsHouseholdExpense { get; private set; }

    public void Update(string name, CategoryType type, bool defaultIsHouseholdExpense)
    {
        ValidateName(name);
        ValidateType(type);

        Name = name;
        Type = type;
        DefaultIsHouseholdExpense = defaultIsHouseholdExpense;
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
