using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.Transactions.Requests;

public sealed class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.FinancialAccountId)
            .NotEmpty();

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
