using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.Users.Requests;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(UserConstraints.NameMaxLength);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(UserConstraints.EmailMaxLength);

        RuleFor(x => x.Password)
            .NotEmpty();

        RuleFor(x => x.HouseholdName)
            .NotEmpty()
            .MaximumLength(Household.NameMaxLength)
            .When(x => string.IsNullOrWhiteSpace(x.InviteCode));
    }
}
