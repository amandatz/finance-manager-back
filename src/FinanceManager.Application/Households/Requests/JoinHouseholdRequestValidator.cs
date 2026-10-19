using FluentValidation;

namespace FinanceManager.Application.Households.Requests;

public sealed class JoinHouseholdRequestValidator : AbstractValidator<JoinHouseholdRequest>
{
    public JoinHouseholdRequestValidator()
    {
        RuleFor(x => x.InviteCode)
            .NotEmpty();
    }
}
