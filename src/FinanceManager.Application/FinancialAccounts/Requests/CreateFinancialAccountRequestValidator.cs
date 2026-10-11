using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.FinancialAccounts.Requests;

public sealed class CreateFinancialAccountRequestValidator : AbstractValidator<CreateFinancialAccountRequest>
{
    public CreateFinancialAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(FinancialAccount.NameMaxLength);

        RuleFor(x => x.InstitutionName)
            .MaximumLength(FinancialAccount.InstitutionNameMaxLength);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
