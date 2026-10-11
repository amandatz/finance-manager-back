using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.FinancialAccounts.Requests;

public sealed class UpdateFinancialAccountRequestValidator : AbstractValidator<UpdateFinancialAccountRequest>
{
    public UpdateFinancialAccountRequestValidator()
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
