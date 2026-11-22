using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.CreditCards.Requests;

public sealed class CreateCreditCardPaymentRequestValidator : AbstractValidator<CreateCreditCardPaymentRequest>
{
    public CreateCreditCardPaymentRequestValidator()
    {
        RuleFor(x => x.FromFinancialAccountId)
            .NotEmpty();

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage("Amount must have at most 2 decimal places.");

        RuleFor(x => x.Currency)
            .Equal(Transaction.SupportedCurrency)
            .When(x => x.Currency is not null);

        RuleFor(x => x.Description)
            .MaximumLength(Transaction.DescriptionMaxLength);
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal amount)
    {
        return decimal.Round(amount, 2) == amount;
    }
}
