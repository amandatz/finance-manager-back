using FinanceManager.Domain.Common;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Entities;

/// <summary>
/// A credit card - a first-class, household-scoped, owned entity in its own right, modeled
/// as a sibling of <see cref="FinancialAccount"/> rather than 1:1-linked to one. A
/// FinancialAccount represents an asset (money you hold); a credit card is a liability (a
/// credit line - its "balance" is what you owe, not what you have), so conflating the two
/// under one base record (even via a 1:1 link) mixes opposite concepts. A store credit card,
/// for instance, has no natural "account" backing it at all.
///
/// Its only relationship to <see cref="FinancialAccount"/> is the optional
/// <see cref="SettlementAccountId"/> - "which account the bill gets paid from" - not a
/// structural parent/child link. See <c>docs/specs/008-contas-cartoes.md</c> section 3 for
/// the full rationale.
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
        Guid? settlementAccountId = null)
        : base(householdId, ownerUserId)
    {
        ValidateBrand(brand);
        ValidateCreditLimit(creditLimit);
        ValidateDay(statementClosingDay, nameof(statementClosingDay));
        ValidateDay(paymentDueDay, nameof(paymentDueDay));
        ValidateDaysDiffer(statementClosingDay, paymentDueDay);

        Brand = brand;
        CreditLimit = creditLimit;
        StatementClosingDay = statementClosingDay;
        PaymentDueDay = paymentDueDay;
        SettlementAccountId = settlementAccountId;
    }

    public string? Brand { get; private set; }

    public decimal CreditLimit { get; private set; }

    public int StatementClosingDay { get; private set; }

    public int PaymentDueDay { get; private set; }

    /// <summary>
    /// Optional FK to the <see cref="FinancialAccount"/> the bill is paid from - a UI
    /// convenience, not a structural requirement (see type-level remarks).
    /// </summary>
    public Guid? SettlementAccountId { get; private set; }

    public void Update(
        decimal creditLimit,
        int statementClosingDay,
        int paymentDueDay,
        string? brand,
        Guid? settlementAccountId)
    {
        ValidateBrand(brand);
        ValidateCreditLimit(creditLimit);
        ValidateDay(statementClosingDay, nameof(statementClosingDay));
        ValidateDay(paymentDueDay, nameof(paymentDueDay));
        ValidateDaysDiffer(statementClosingDay, paymentDueDay);

        Brand = brand;
        CreditLimit = creditLimit;
        StatementClosingDay = statementClosingDay;
        PaymentDueDay = paymentDueDay;
        SettlementAccountId = settlementAccountId;
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

    private static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
}
