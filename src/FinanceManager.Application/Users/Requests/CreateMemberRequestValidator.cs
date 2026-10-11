using FluentValidation;

namespace FinanceManager.Application.Users.Requests;

public sealed class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberRequestValidator()
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
    }
}
