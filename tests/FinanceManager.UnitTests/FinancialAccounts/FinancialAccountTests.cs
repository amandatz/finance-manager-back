using FinanceManager.Domain.Entities;
using FinanceManager.Domain.Enums;

namespace FinanceManager.UnitTests.FinancialAccounts;

public class FinancialAccountTests
{
    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => new FinancialAccount(string.Empty, FinancialAccountType.Bank);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WithNameOver100Chars_ThrowsArgumentException()
    {
        var name = new string('a', FinancialAccount.NameMaxLength + 1);

        var act = () => new FinancialAccount(name, FinancialAccountType.Bank);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WithInstitutionNameOver150Chars_ThrowsArgumentException()
    {
        var institutionName = new string('a', FinancialAccount.InstitutionNameMaxLength + 1);

        var act = () => new FinancialAccount("Checking", FinancialAccountType.Bank, institutionName);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_WithUndefinedType_ThrowsArgumentException()
    {
        var act = () => new FinancialAccount("Checking", (FinancialAccountType)999);

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Update_WithEmptyName_ThrowsArgumentException()
    {
        var account = new FinancialAccount("Checking", FinancialAccountType.Bank);

        var act = () => account.Update(string.Empty, FinancialAccountType.Bank, null);

        Assert.Throws<ArgumentException>(act);
    }
}
