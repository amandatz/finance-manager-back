using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.CreditCards.Requests;

public sealed class CreateCreditCardRequestValidator : AbstractValidator<CreateCreditCardRequest>
{
    public CreateCreditCardRequestValidator()
    {
        RuleFor(x => x.CreditLimit)
            .GreaterThan(0);

        RuleFor(x => x.StatementClosingDay)
            .InclusiveBetween(CreditCard.MinDay, CreditCard.MaxDay);

        RuleFor(x => x.PaymentDueDay)
            .InclusiveBetween(CreditCard.MinDay, CreditCard.MaxDay);

        RuleFor(x => x.Brand)
            .MaximumLength(CreditCard.BrandMaxLength);
    }
}
