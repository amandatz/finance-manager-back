using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.Households.Requests;

public sealed class CreateHouseholdRequestValidator : AbstractValidator<CreateHouseholdRequest>
{
    public CreateHouseholdRequestValidator()
    {
        RuleFor(x => x.HouseholdName)
            .NotEmpty()
            .MaximumLength(Household.NameMaxLength);
    }
}
