using Blocko.Services.Interfaces.Product;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Interfaces;

namespace Blocko.Services.Implementations.Product
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        public ProductService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable
            });
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var p = await _unitOfWork.Products.GetByIdAsync(id);
            if (p == null) return null;
            return new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable
            };
        }

        public async Task<IEnumerable<ProductDto>> GetProductsByCategoryAsync(int categoryId)
        {
            var products = await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable
            });
        }

        public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync()
        {
            var products = await _unitOfWork.Products.GetFeaturedProductsAsync();
            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable
            });
        }

        public async Task AddProductAsync(ProductDto productDto)
        {
            var product = new Bolcko.Domain.Entities.Product.Product
            {
                Name = productDto.Name,
                Description = productDto.Description,
                CategoryId = productDto.CategoryId,
                RetailPrice = productDto.RetailPrice,
                StockQuantity = productDto.StockQuantity,
                UnitOfMeasure = productDto.UnitOfMeasure,
                Sku = productDto.Sku,
                ImageUrl = productDto.ImageUrl,
                BulkPricingAvailable = productDto.BulkPricingAvailable
            };
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(ProductDto productDto)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productDto.Id);
            if (product != null)
            {
                product.Name = productDto.Name;
                product.Description = productDto.Description;
                product.CategoryId = productDto.CategoryId;
                product.RetailPrice = productDto.RetailPrice;
                product.StockQuantity = productDto.StockQuantity;
                product.UnitOfMeasure = productDto.UnitOfMeasure;
                product.Sku = productDto.Sku;
                product.ImageUrl = productDto.ImageUrl;
                product.BulkPricingAvailable = productDto.BulkPricingAvailable;
                _unitOfWork.Products.Update(product);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product != null)
            {
                _unitOfWork.Products.Remove(product);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}