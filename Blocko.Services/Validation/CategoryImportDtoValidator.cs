using FluentValidation;
using Bolcko.Domain.Entities.Catalog.DTOs;

namespace Blocko.Services.Validation
{
    public class CategoryImportDtoValidator : AbstractValidator<CategoryImportDto>
    {
        public CategoryImportDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.")
                .MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");

            RuleFor(x => x.DisplayOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0.");
        }
    }
}
