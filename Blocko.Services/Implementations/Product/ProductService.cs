using Blocko.Persistence.Common;
using Blocko.Services.Interfaces.Product;
using Bolcko.Domain.Common;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

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

        public async Task<IPagedList<ProductDto>> GetPagedProductsAsync(int pageIndex, int pageSize)
        {
            var pagedProducts = await _unitOfWork.Products.GetPagedAsync(
                pageIndex,
                pageSize,
                orderBy: q => q.OrderByDescending(p => p.Id),
                includes: p => p.Category!
            );

            var dtos = pagedProducts.Items.Select(p => new ProductDto
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

            return new PagedList<ProductDto>(dtos, pagedProducts.TotalCount, pageIndex, pageSize);
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var p = await _unitOfWork.Products.GetByIdWithImagesAsync(id);
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
                BulkPricingAvailable = p.BulkPricingAvailable,
                Images = p.Images.Select(img => new ProductImageDto
                {
                    Id = img.Id,
                    Url = img.Url,
                    AltText = img.AltText,
                    Caption = img.Caption,
                    DisplayOrder = img.DisplayOrder
                }).ToList()
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

        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string? query)
        {
            var products = await _unitOfWork.Products.SearchProductsAsync(query);
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

        public async Task<IPagedList<ProductDto>> SearchCatalogProductsPagedAsync(string? query, int? categoryId, int pageIndex, int pageSize)
        {
            var searchTerm = (query ?? string.Empty).Trim();
            var hasSearch = !string.IsNullOrEmpty(searchTerm);
            var pattern = $"%{searchTerm}%";

            var pagedProducts = await _unitOfWork.Products.GetPagedAsync(
                pageIndex: pageIndex,
                pageSize: pageSize,
                predicate: p =>
                    (!categoryId.HasValue || p.CategoryId == categoryId.Value || p.Category!.ParentCategoryId == categoryId.Value) &&
                    (!hasSearch || EF.Functions.Like(p.Name, pattern) || (p.Sku != null && EF.Functions.Like(p.Sku, pattern))),
                orderBy: q => q.OrderByDescending(p => p.Id),
                includes: p => p.Category!
            );

            var dtos = pagedProducts.Items.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl
            });

            return new PagedList<ProductDto>(dtos, pagedProducts.TotalCount, pageIndex, pageSize);
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
                BulkPricingAvailable = productDto.BulkPricingAvailable,
                Images = productDto.Images.Select(img => new ProductImage
                {
                    Url = img.Url,
                    AltText = img.AltText ?? productDto.Name,
                    Caption = img.Caption,
                    DisplayOrder = img.DisplayOrder
                }).ToList()
            };
            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();
        }

        public async Task UpdateProductAsync(ProductDto productDto, List<int>? deleteImageIds = null)
        {
            var product = await _unitOfWork.Products.GetByIdWithImagesAsync(productDto.Id);
            if (product != null)
            {
                product.Name = productDto.Name;
                product.Description = productDto.Description;
                product.CategoryId = productDto.CategoryId;
                product.RetailPrice = productDto.RetailPrice;
                product.StockQuantity = productDto.StockQuantity;
                product.UnitOfMeasure = productDto.UnitOfMeasure;
                product.Sku = productDto.Sku;
                product.BulkPricingAvailable = productDto.BulkPricingAvailable;

                // Handle deletions
                if (deleteImageIds != null && deleteImageIds.Any())
                {
                    var imagesToRemove = product.Images.Where(img => deleteImageIds.Contains(img.Id)).ToList();
                    foreach (var img in imagesToRemove)
                    {
                        product.Images.Remove(img);
                    }
                }

                // Add new images
                if (productDto.Images != null && productDto.Images.Any())
                {
                    int nextOrder = product.Images.Any() ? product.Images.Max(img => img.DisplayOrder) + 1 : 1;
                    foreach (var imgDto in productDto.Images)
                    {
                        product.Images.Add(new ProductImage
                        {
                            Url = imgDto.Url,
                            AltText = imgDto.AltText ?? productDto.Name,
                            Caption = imgDto.Caption,
                            DisplayOrder = nextOrder++
                        });
                    }
                }

                // Update ImageUrl
                if (product.Images.Any())
                {
                    product.ImageUrl = product.Images.OrderBy(img => img.DisplayOrder).First().Url;
                }
                else
                {
                    product.ImageUrl = null;
                }

                _unitOfWork.Products.Update(product);
                await _unitOfWork.CompleteAsync();
            }
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product != null)
            {
                _unitOfWork.Products.Remove(product);
                await _unitOfWork.CompleteAsync();
            }
        }
    }
}