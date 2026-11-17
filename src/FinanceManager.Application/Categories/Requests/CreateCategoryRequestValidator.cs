using FinanceManager.Domain.Entities;
using FluentValidation;

namespace FinanceManager.Application.Categories.Requests;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(Category.NameMaxLength);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
