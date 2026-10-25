using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.UnitTests.FinancialAccounts;

public class FinancialAccountTests
{
    private static readonly Guid HouseholdId = Guid.NewGuid();
    private static readonly Guid OwnerUserId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithEmptyName_ThrowsDomainValidationException()
    {
        var act = () => new FinancialAccount(string.Empty, FinancialAccountType.Bank, HouseholdId, OwnerUserId);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithNameOver100Chars_ThrowsDomainValidationException()
    {
        var name = new string('a', FinancialAccount.NameMaxLength + 1);

        var act = () => new FinancialAccount(name, FinancialAccountType.Bank, HouseholdId, OwnerUserId);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithInstitutionNameOver150Chars_ThrowsDomainValidationException()
    {
        var institutionName = new string('a', FinancialAccount.InstitutionNameMaxLength + 1);

        var act = () => new FinancialAccount(
            "Checking", FinancialAccountType.Bank, HouseholdId, OwnerUserId, institutionName);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_WithUndefinedType_ThrowsDomainValidationException()
    {
        var act = () => new FinancialAccount("Checking", (FinancialAccountType)999, HouseholdId, OwnerUserId);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void Constructor_SetsCreatingUserAsSoleOwner()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, HouseholdId, OwnerUserId);

        Assert.Single(account.OwnerUserIds);
        Assert.True(account.IsOwnedBy(OwnerUserId));
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsDomainValidationException()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, HouseholdId, OwnerUserId);

        var act = () => account.Update(string.Empty, FinancialAccountType.Bank, null);

        Assert.Throws<DomainValidationException>(act);
    }

    [Fact]
    public void AddOwner_WithNewUserId_AddsIt()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, HouseholdId, OwnerUserId);
        var secondOwner = Guid.NewGuid();

        account.AddOwner(secondOwner);

        Assert.Equal(2, account.OwnerUserIds.Count);
        Assert.True(account.IsOwnedBy(secondOwner));
    }

    [Fact]
    public void AddOwner_WithExistingUserId_DoesNotDuplicate()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, HouseholdId, OwnerUserId);

        account.AddOwner(OwnerUserId);

        Assert.Single(account.OwnerUserIds);
    }

    [Fact]
    public void RemoveOwner_WithSoleOwner_ThrowsBusinessRuleViolationException()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, HouseholdId, OwnerUserId);

        var act = () => account.RemoveOwner(OwnerUserId);

        Assert.Throws<BusinessRuleViolationException>(act);
    }

    [Fact]
    public void RemoveOwner_WithMultipleOwners_RemovesIt()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank, HouseholdId, OwnerUserId);
        var secondOwner = Guid.NewGuid();
        account.AddOwner(secondOwner);

        account.RemoveOwner(secondOwner);

        Assert.Single(account.OwnerUserIds);
        Assert.False(account.IsOwnedBy(secondOwner));
    }
}
