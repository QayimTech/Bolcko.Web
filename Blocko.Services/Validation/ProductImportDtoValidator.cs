using FluentValidation;
using Bolcko.Domain.Entities.Product.DTOs;

namespace Blocko.Services.Validation
{
    public class ProductImportDtoValidator : AbstractValidator<ProductImportDto>
    {
        public ProductImportDtoValidator()
        {
            RuleFor(x => x.Sku)
                .NotEmpty().WithMessage("SKU is required.");
            
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("Product name must not exceed 100 characters.");
            
            RuleFor(x => x.CategoryName)
                .NotEmpty().WithMessage("Category name is required.");

            RuleFor(x => x.RetailPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Retail price must be greater than or equal to 0.");

            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be greater than or equal to 0.");

            RuleFor(x => x.Weight)
                .GreaterThanOrEqualTo(0).When(x => x.Weight.HasValue).WithMessage("Weight must be positive.");
        }
    }
}
