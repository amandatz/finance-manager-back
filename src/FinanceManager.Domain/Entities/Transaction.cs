using FinanceManager.Domain.Common;
using FinanceManager.Domain.Enums;
using FinanceManager.Domain.Exceptions;

namespace FinanceManager.Domain.Entities;

/// <summary>
/// An income or expense movement against a <see cref="FinancialAccount"/>. Household-scoped like
/// its parent account, but has no individual owner of its own - <see cref="RecordedByUserId"/>
/// is who logged it, which is traceability, not ownership.
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
        : base(householdId)
    {
        ValidateAmount(amount);
        ValidateCurrency(currency);
        ValidateDescription(description);

        FinancialAccountId = financialAccountId;
        CategoryId = categoryId;
        RecordedByUserId = recordedByUserId;
        Type = type;
        Amount = amount;
        Currency = currency;
        Date = date;
        Description = description;
        IsHouseholdExpense = isHouseholdExpense;
    }

    public Guid FinancialAccountId { get; private set; }

    public Guid CategoryId { get; private set; }

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

    public void Update(
        Guid categoryId,
        TransactionType type,
        decimal amount,
        string currency,
        DateOnly date,
        string? description,
        bool? isHouseholdExpense)
    {
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
