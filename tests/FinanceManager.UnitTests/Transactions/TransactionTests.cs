using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.UnitTests.Transactions;

public class TransactionTests
{
    private static readonly Guid HouseholdId = Guid.NewGuid();
    private static readonly Guid FinancialAccountId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid RecordedByUserId = Guid.NewGuid();
    private static readonly DateOnly Date = new(2026, 11, 16);

    private static Transaction CreateValidTransaction(
        decimal amount = 100m,
        string currency = "BRL",
        string? description = null) =>
        new(
            FinancialAccountId,
            CategoryId,
            RecordedByUserId,
            TransactionType.Expense,
            amount,
            currency,
            Date,
            HouseholdId,
            description);

    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        var transaction = CreateValidTransaction();

        Assert.Equal(FinancialAccountId, transaction.FinancialAccountId);
        Assert.Equal(CategoryId, transaction.CategoryId);
        Assert.Equal(RecordedByUserId, transaction.RecordedByUserId);
        Assert.Equal(TransactionType.Expense, transaction.Type);
        Assert.Equal(100m, transaction.Amount);
        Assert.Equal("BRL", transaction.Currency);
        Assert.Equal(Date, transaction.Date);
        Assert.Equal(HouseholdId, transaction.HouseholdId);
        Assert.Null(transaction.IsHouseholdExpense);
        Assert.True(transaction.IsActive);
    }

    [Fact]
    public void Constructor_WithZeroAmount_ThrowsDomainValidationException()
    {
        var act = () => CreateValidTransaction(amount: 0m);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsDomainValidationException()
    {
        var act = () => CreateValidTransaction(amount: -10m);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithNonBrlCurrency_ThrowsDomainValidationException()
    {
        var act = () => CreateValidTransaction(currency: "USD");

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithDescriptionOver250Chars_ThrowsDomainValidationException()
    {
        var description = new string('a', Transaction.DescriptionMaxLength + 1);

        var act = () => CreateValidTransaction(description: description);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithIsHouseholdExpenseSet_RoundTripsValueUnused()
    {
        var transaction = new Transaction(
            FinancialAccountId,
            CategoryId,
            RecordedByUserId,
            TransactionType.Expense,
            100m,
            "BRL",
            Date,
            HouseholdId,
            isHouseholdExpense: true);

        Assert.True(transaction.IsHouseholdExpense);
    }

    [Fact]
    public void Update_WithValidData_UpdatesFieldsAndMarksUpdated()
    {
        var transaction = CreateValidTransaction();
        var newCategoryId = Guid.NewGuid();
        var newDate = new DateOnly(2026, 12, 1);

        transaction.Update(newCategoryId, TransactionType.Income, 200m, "BRL", newDate, "Updated", true);

        Assert.Equal(newCategoryId, transaction.CategoryId);
        Assert.Equal(TransactionType.Income, transaction.Type);
        Assert.Equal(200m, transaction.Amount);
        Assert.Equal(newDate, transaction.Date);
        Assert.Equal("Updated", transaction.Description);
        Assert.True(transaction.IsHouseholdExpense);
        Assert.NotNull(transaction.UpdatedAt);
    }

    [Fact]
    public void Update_WithZeroAmount_ThrowsDomainValidationException()
    {
        var transaction = CreateValidTransaction();

        var act = () => transaction.Update(CategoryId, TransactionType.Expense, 0m, "BRL", Date, null, null);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Update_WithNonBrlCurrency_ThrowsDomainValidationException()
    {
        var transaction = CreateValidTransaction();

        var act = () => transaction.Update(CategoryId, TransactionType.Expense, 100m, "USD", Date, null, null);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalseAndMarksUpdated()
    {
        var transaction = CreateValidTransaction();

        transaction.Deactivate();

        Assert.False(transaction.IsActive);
        Assert.NotNull(transaction.UpdatedAt);
    }
}
