using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.Transactions.Requests;

public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.FinancialAccountId.HasValue ^ x.CreditCardId.HasValue)
            .WithMessage("Inform exactly one of FinancialAccountId or CreditCardId.");

        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage("Amount must have at most 2 decimal places.");

        RuleFor(x => x.Currency)
            .Equal(Transaction.SupportedCurrency);

        RuleFor(x => x.Description)
            .MaximumLength(Transaction.DescriptionMaxLength);
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal amount)
    {
        return decimal.Round(amount, 2) == amount;
    }
}
