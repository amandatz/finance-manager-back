using FinanceManager.Domain.Common;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Entities;

/// <summary>
/// A credit card: a liability, modeled as a sibling of <see cref="FinancialAccount"/> rather than linked 1:1 to one.
/// </summary>
public sealed class CreditCard : HouseholdScopedOwnedEntity
{
    public const int BrandMaxLength = 50;
    public const int MinDay = 1;
    public const int MaxDay = 31;

    private CreditCard()
    {
    }

    public CreditCard(
        Guid householdId,
        Guid ownerUserId,
        decimal creditLimit,
        int statementClosingDay,
        int paymentDueDay,
        string? brand = null,
        Guid? settlementAccountId = null,
        CreditCardColor color = CreditCardColor.Graphite)
        : base(householdId, ownerUserId)
    {
        ValidateBrand(brand);
        ValidateCreditLimit(creditLimit);
        ValidateDay(statementClosingDay, nameof(statementClosingDay));
        ValidateDay(paymentDueDay, nameof(paymentDueDay));
        ValidateDaysDiffer(statementClosingDay, paymentDueDay);
        ValidateColor(color);

        Brand = brand;
        CreditLimit = creditLimit;
        StatementClosingDay = statementClosingDay;
        PaymentDueDay = paymentDueDay;
        SettlementAccountId = settlementAccountId;
        Color = color;
    }

    public string? Brand { get; private set; }

    public decimal CreditLimit { get; private set; }

    public int StatementClosingDay { get; private set; }

    public int PaymentDueDay { get; private set; }

    /// <summary>
    /// Optional FK to the <see cref="FinancialAccount"/> the bill is paid from.
    /// </summary>
    public Guid? SettlementAccountId { get; private set; }

    public CreditCardColor Color { get; private set; }

    public void Update(
        decimal creditLimit,
        int statementClosingDay,
        int paymentDueDay,
        string? brand,
        Guid? settlementAccountId,
        CreditCardColor color = CreditCardColor.Graphite)
    {
        ValidateBrand(brand);
        ValidateCreditLimit(creditLimit);
        ValidateDay(statementClosingDay, nameof(statementClosingDay));
        ValidateDay(paymentDueDay, nameof(paymentDueDay));
        ValidateDaysDiffer(statementClosingDay, paymentDueDay);
        ValidateColor(color);

        Brand = brand;
        CreditLimit = creditLimit;
        StatementClosingDay = statementClosingDay;
        PaymentDueDay = paymentDueDay;
        SettlementAccountId = settlementAccountId;
        Color = color;
        MarkAsUpdated();
    }

    private static void ValidateBrand(string? brand)
    {
        if (brand is { Length: > BrandMaxLength })
        {
            throw new DomainValidationException(
                $"Brand must not exceed {BrandMaxLength} characters.",
                nameof(brand));
        }
    }

    private static void ValidateCreditLimit(decimal creditLimit)
    {
        if (creditLimit <= 0)
        {
            throw new DomainValidationException("CreditLimit must be greater than zero.", nameof(creditLimit));
        }
    }

    private static void ValidateDay(int day, string paramName)
    {
        if (day is < MinDay or > MaxDay)
        {
            throw new DomainValidationException(
                $"{Capitalize(paramName)} must be between {MinDay} and {MaxDay}.",
                paramName);
        }
    }

    private static void ValidateDaysDiffer(int statementClosingDay, int paymentDueDay)
    {
        if (statementClosingDay == paymentDueDay)
        {
            throw new DomainValidationException(
                "PaymentDueDay must be different from StatementClosingDay.",
                nameof(paymentDueDay));
        }
    }

    private static void ValidateColor(CreditCardColor color)
    {
        if (!Enum.IsDefined(color))
        {
            throw new DomainValidationException("Color is not a valid CreditCardColor.", nameof(color));
        }
    }

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
