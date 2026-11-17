using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.UnitTests.Categories;

public class CategoryTests
{
    private static readonly Guid HouseholdId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        var category = new Category("Alimentação", CategoryType.Expense, HouseholdId);

        Assert.Equal("Alimentação", category.Name);
        Assert.Equal(CategoryType.Expense, category.Type);
        Assert.Equal(HouseholdId, category.HouseholdId);
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsDomainValidationException()
    {
        var act = () => new Category(string.Empty, CategoryType.Expense, HouseholdId);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithNameOver100Chars_ThrowsDomainValidationException()
    {
        var name = new string('a', Category.NameMaxLength + 1);

        var act = () => new Category(name, CategoryType.Expense, HouseholdId);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithUndefinedType_ThrowsDomainValidationException()
    {
        var act = () => new Category("Alimentação", (CategoryType)999, HouseholdId);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Update_WithValidData_UpdatesFieldsAndMarksUpdated()
    {
        var category = new Category("Alimentação", CategoryType.Expense, HouseholdId);

        category.Update("Mercado", CategoryType.Expense);

        Assert.Equal("Mercado", category.Name);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsDomainValidationException()
    {
        var category = new Category("Alimentação", CategoryType.Expense, HouseholdId);

        var act = () => category.Update(string.Empty, CategoryType.Expense);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalseAndMarksUpdated()
    {
        var category = new Category("Alimentação", CategoryType.Expense, HouseholdId);

        category.Deactivate();

        Assert.False(category.IsActive);
        Assert.NotNull(category.UpdatedAt);
    }
}
