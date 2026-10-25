using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.UnitTests.CreditCards;

public class CreditCardTests
{
    private static readonly Guid HouseholdId = Guid.NewGuid();
    private static readonly Guid OwnerUserId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_Succeeds()
    {
        var card = new CreditCard(HouseholdId, OwnerUserId, 1000m, 10, 20, "Visa");

        Assert.Equal(1000m, card.CreditLimit);
        Assert.Equal(10, card.StatementClosingDay);
        Assert.Equal(20, card.PaymentDueDay);
        Assert.Equal("Visa", card.Brand);
        Assert.True(card.IsOwnedBy(OwnerUserId));
        Assert.Equal(HouseholdId, card.HouseholdId);
    }

    [Fact]
    public void Constructor_WithZeroCreditLimit_ThrowsDomainValidationException()
    {
        var act = () => new CreditCard(HouseholdId, OwnerUserId, 0m, 10, 20);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithNegativeCreditLimit_ThrowsDomainValidationException()
    {
        var act = () => new CreditCard(HouseholdId, OwnerUserId, -1m, 10, 20);

        Assert.Throws<DomainValidationException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Constructor_WithStatementClosingDayOutOfRange_ThrowsDomainValidationException(int day)
    {
        var act = () => new CreditCard(HouseholdId, OwnerUserId, 1000m, day, 20);

        Assert.Throws<DomainValidationException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void Constructor_WithPaymentDueDayOutOfRange_ThrowsDomainValidationException(int day)
    {
        var act = () => new CreditCard(HouseholdId, OwnerUserId, 1000m, 10, day);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithSameClosingAndDueDay_ThrowsDomainValidationException()
    {
        var act = () => new CreditCard(HouseholdId, OwnerUserId, 1000m, 10, 10);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithBrandOver50Chars_ThrowsDomainValidationException()
    {
        var brand = new string('a', CreditCard.BrandMaxLength + 1);

        var act = () => new CreditCard(HouseholdId, OwnerUserId, 1000m, 10, 20, brand);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Update_WithValidData_UpdatesFieldsAndMarksUpdated()
    {
        var card = new CreditCard(HouseholdId, OwnerUserId, 1000m, 10, 20, "Visa");

        card.Update(2000m, 5, 15, "Mastercard", null);

        Assert.Equal(2000m, card.CreditLimit);
        Assert.Equal(5, card.StatementClosingDay);
        Assert.Equal(15, card.PaymentDueDay);
        Assert.Equal("Mastercard", card.Brand);
        Assert.NotNull(card.UpdatedAt);
    }

    [Fact]
    public void Update_WithSameClosingAndDueDay_ThrowsDomainValidationException()
    {
        var card = new CreditCard(HouseholdId, OwnerUserId, 1000m, 10, 20);

        var act = () => card.Update(1000m, 15, 15, null, null);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void RemoveOwner_WithSoleOwner_ThrowsBusinessRuleViolationException()
    {
        var card = new CreditCard(HouseholdId, OwnerUserId, 1000m, 10, 20);

        var act = () => card.RemoveOwner(OwnerUserId);

        Assert.Throws<BusinessRuleViolationException>(act);
    }
}
