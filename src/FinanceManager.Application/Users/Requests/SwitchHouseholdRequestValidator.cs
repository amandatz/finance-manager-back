using FluentValidation;

namespace FinanceManager.Application.Users.Requests;

public sealed class SwitchHouseholdRequestValidator : AbstractValidator<SwitchHouseholdRequest>
{
    public SwitchHouseholdRequestValidator()
    {
        RuleFor(x => x.HouseholdId)
            .NotEmpty();
    }
}
