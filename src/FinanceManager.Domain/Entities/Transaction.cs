using FinanceManager.Domain.Common;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Entities;

/// <summary>
/// An income or expense movement against a <see cref="FinancialAccount"/> or a <see cref="CreditCard"/>
/// (exactly one of the two - never both, never neither). Household-scoped like its target, but has no
/// individual owner of its own - <see cref="RecordedByUserId"/> is who logged it, which is traceability,
/// not ownership.
/// </summary>
public sealed class Transaction : HouseholdScopedEntity
{
    public const int DescriptionMaxLength = 250;
    public const string SupportedCurrency = "BRL";

    private Transaction()
    {
    }

    public Transaction(
        Guid financialAccountId,
        Guid categoryId,
        Guid recordedByUserId,
        TransactionType type,
        decimal amount,
        string currency,
        DateOnly date,
        Guid householdId,
        string? description = null,
        bool? isHouseholdExpense = null)
        : this(
            financialAccountId,
            creditCardId: null,
            categoryId,
            recordedByUserId,
            type,
            amount,
            currency,
            date,
            householdId,
            description,
            isHouseholdExpense,
            settlementGroupId: null)
    {
    }

    public static Transaction ForCreditCard(
        Guid creditCardId,
        Guid categoryId,
        Guid recordedByUserId,
        TransactionType type,
        decimal amount,
        string currency,
        DateOnly date,
        Guid householdId,
        string? description = null,
        bool? isHouseholdExpense = null) =>
        new(
            financialAccountId: null,
            creditCardId,
            categoryId,
            recordedByUserId,
            type,
            amount,
            currency,
            date,
            householdId,
            description,
            isHouseholdExpense,
            settlementGroupId: null);

    /// <summary>
    /// The account-side leg of a credit card bill payment: an <see cref="TransactionType.Expense"/>
    /// against <paramref name="financialAccountId"/>, carrying no category - settlement legs are not
    /// categorized spend, they are identified by <see cref="SettlementGroupId"/> and excluded from
    /// category spend reports.
    /// </summary>
    public static Transaction CreateAccountSettlementLeg(
        Guid financialAccountId,
        Guid recordedByUserId,
        decimal amount,
        string currency,
        DateOnly date,
        Guid householdId,
        Guid settlementGroupId,
        string? description = null) =>
        new(
            financialAccountId,
            creditCardId: null,
            categoryId: null,
            recordedByUserId,
            TransactionType.Expense,
            amount,
            currency,
            date,
            householdId,
            description,
            isHouseholdExpense: null,
            settlementGroupId);

    /// <summary>
    /// The credit card-side leg of a bill payment: an <see cref="TransactionType.Income"/> against
    /// <paramref name="creditCardId"/>, reducing its <c>currentDebt</c>. See <see cref="CreateAccountSettlementLeg"/>.
    /// </summary>
    public static Transaction CreateCreditCardSettlementLeg(
        Guid creditCardId,
        Guid recordedByUserId,
        decimal amount,
        string currency,
        DateOnly date,
        Guid householdId,
        Guid settlementGroupId,
        string? description = null) =>
        new(
            financialAccountId: null,
            creditCardId,
            categoryId: null,
            recordedByUserId,
            TransactionType.Income,
            amount,
            currency,
            date,
            householdId,
            description,
            isHouseholdExpense: null,
            settlementGroupId);

    private Transaction(
        Guid? financialAccountId,
        Guid? creditCardId,
        Guid? categoryId,
        Guid recordedByUserId,
        TransactionType type,
        decimal amount,
        string currency,
        DateOnly date,
        Guid householdId,
        string? description,
        bool? isHouseholdExpense,
        Guid? settlementGroupId)
        : base(householdId)
    {
        ValidateTarget(financialAccountId, creditCardId);
        ValidateCategory(categoryId, settlementGroupId);
        ValidateAmount(amount);
        ValidateCurrency(currency);
        ValidateDescription(description);

        FinancialAccountId = financialAccountId;
        CreditCardId = creditCardId;
        CategoryId = categoryId;
        RecordedByUserId = recordedByUserId;
        Type = type;
        Amount = amount;
        Currency = currency;
        Date = date;
        Description = description;
        IsHouseholdExpense = isHouseholdExpense;
        SettlementGroupId = settlementGroupId;
    }

    public Guid? FinancialAccountId { get; private set; }

    public Guid? CreditCardId { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    public TransactionType Type { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = null!;

    public DateOnly Date { get; private set; }

    public string? Description { get; private set; }

    /// <summary>
    /// Reserved for a future household-expense-split feature; carries no business rule yet.
    /// </summary>
    public bool? IsHouseholdExpense { get; private set; }

    /// <summary>
    /// Links the two legs of the same credit card bill payment. Null for ordinary
    /// transactions (purchases, income). Both legs sharing this id are archived together.
    /// </summary>
    public Guid? SettlementGroupId { get; private set; }

    public bool IsSettlementLeg => SettlementGroupId is not null;

    public void Update(
        Guid categoryId,
        TransactionType type,
        decimal amount,
        string currency,
        DateOnly date,
        string? description,
        bool? isHouseholdExpense)
    {
        if (IsSettlementLeg)
        {
            throw new BusinessRuleViolationException("A settlement leg cannot be edited directly.");
        }

        ValidateAmount(amount);
        ValidateCurrency(currency);
        ValidateDescription(description);

        CategoryId = categoryId;
        Type = type;
        Amount = amount;
        Currency = currency;
        Date = date;
        Description = description;
        IsHouseholdExpense = isHouseholdExpense;
        MarkAsUpdated();
    }

    private static void ValidateTarget(Guid? financialAccountId, Guid? creditCardId)
    {
        var targetCount = (financialAccountId.HasValue ? 1 : 0) + (creditCardId.HasValue ? 1 : 0);

        if (targetCount != 1)
        {
            throw new BusinessRuleViolationException(
                "Exactly one of FinancialAccountId or CreditCardId must be provided.");
        }
    }

    private static void ValidateCategory(Guid? categoryId, Guid? settlementGroupId)
    {
        if (settlementGroupId is null && categoryId is null)
        {
            throw new DomainValidationException("CategoryId is required.", nameof(categoryId));
        }

        if (settlementGroupId is not null && categoryId is not null)
        {
            throw new BusinessRuleViolationException("A settlement leg must not carry a CategoryId.");
        }
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new DomainValidationException("Amount must be greater than zero.", nameof(amount));
        }
    }

    private static void ValidateCurrency(string currency)
    {
        if (!string.Equals(currency, SupportedCurrency, StringComparison.Ordinal))
        {
            throw new DomainValidationException(
                $"Currency must be '{SupportedCurrency}'.",
                nameof(currency));
        }
    }

    private static void ValidateDescription(string? description)
    {
        if (description is { Length: > DescriptionMaxLength })
        {
            throw new DomainValidationException(
                $"Description must not exceed {DescriptionMaxLength} characters.",
                nameof(description));
        }
    }
}
