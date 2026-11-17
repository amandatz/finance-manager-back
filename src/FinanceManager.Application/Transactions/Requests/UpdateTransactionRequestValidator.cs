using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.Transactions.Requests;

public sealed class UpdateTransactionRequestValidator : AbstractValidator<UpdateTransactionRequest>
{
    public UpdateTransactionRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty();

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.Currency)
            .Equal(Transaction.SupportedCurrency);

        RuleFor(x => x.Description)
            .MaximumLength(Transaction.DescriptionMaxLength);
    }
}
