using Blocko.Persistence.Common;
using Blocko.Services.Interfaces;
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
                NameEn = p.NameEn,
                Description = p.Description,
                DescriptionEn = p.DescriptionEn,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable,
                UpdatedAt = p.UpdatedAt
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
                NameEn = p.NameEn,
                Description = p.Description,
                DescriptionEn = p.DescriptionEn,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable,
                UpdatedAt = p.UpdatedAt
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
                NameEn = p.NameEn,
                Description = p.Description,
                DescriptionEn = p.DescriptionEn,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable,
                UpdatedAt = p.UpdatedAt,
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
                NameEn = p.NameEn,
                Description = p.Description,
                DescriptionEn = p.DescriptionEn,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable,
                UpdatedAt = p.UpdatedAt
            });
        }

        public async Task<IEnumerable<ProductDto>> GetFeaturedProductsAsync()
        {
            var products = await _unitOfWork.Products.GetFeaturedProductsAsync();
            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                NameEn = p.NameEn,
                Description = p.Description,
                DescriptionEn = p.DescriptionEn,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable,
                UpdatedAt = p.UpdatedAt
            });
        }

        public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string? query)
        {
            var products = await _unitOfWork.Products.SearchProductsAsync(query);
            return products.Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                NameEn = p.NameEn,
                Description = p.Description,
                DescriptionEn = p.DescriptionEn,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                BulkPricingAvailable = p.BulkPricingAvailable,
                UpdatedAt = p.UpdatedAt
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
                NameEn = p.NameEn,
                Description = p.Description,
                DescriptionEn = p.DescriptionEn,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name,
                RetailPrice = p.RetailPrice,
                StockQuantity = p.StockQuantity,
                UnitOfMeasure = p.UnitOfMeasure,
                Sku = p.Sku,
                ImageUrl = p.ImageUrl,
                Brand = p.Brand,
                CountryOfOrigin = p.CountryOfOrigin,
                BulkPricingAvailable = p.BulkPricingAvailable,
                UpdatedAt = p.UpdatedAt
            });

            return new PagedList<ProductDto>(dtos, pagedProducts.TotalCount, pageIndex, pageSize);
        }

        public async Task AddProductAsync(ProductDto productDto)
        {
            var product = new Bolcko.Domain.Entities.Product.Product
            {
                Name = productDto.Name,
                NameEn = productDto.NameEn,
                Description = productDto.Description,
                DescriptionEn = productDto.DescriptionEn,
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
                product.NameEn = productDto.NameEn;
                product.Description = productDto.Description;
                product.DescriptionEn = productDto.DescriptionEn;
                product.CategoryId = productDto.CategoryId;
                product.RetailPrice = productDto.RetailPrice;
                product.StockQuantity = productDto.StockQuantity;
                product.UnitOfMeasure = productDto.UnitOfMeasure;
                product.Sku = productDto.Sku;
                product.BulkPricingAvailable = productDto.BulkPricingAvailable;
                product.UpdatedAt = DateTime.UtcNow;

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

        /// <summary>
        /// One-time bulk translation: translates all products where NameEn is null/empty.
        /// Uses the override dictionary first (instant, free), then falls back to Google API.
        /// Processes in batches of 10 with a 300ms delay to respect rate limits.
        /// </summary>
        public async Task<(int translated, int skipped, int failed)> BulkTranslateAsync(ITranslationService translationService)
        {
            var products = await _unitOfWork.Products.GetAllAsQueryable()
                .Where(p => string.IsNullOrEmpty(p.NameEn))
                .ToListAsync();

            int translated = 0, skipped = 0, failed = 0;
            const int batchSize = 10;

            for (int i = 0; i < products.Count; i += batchSize)
            {
                var batch = products.Skip(i).Take(batchSize).ToList();

                foreach (var product in batch)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(product.Name))
                        {
                            skipped++;
                            continue;
                        }

                        var nameEn = await translationService.TranslateAsync(product.Name, "en");

                        // If translation returned same Arabic text (API failed/blocked), skip to avoid bad data
                        bool isStillArabic = System.Text.RegularExpressions.Regex.IsMatch(nameEn, @"[\u0600-\u06FF]");
                        if (isStillArabic)
                        {
                            failed++;
                            continue;
                        }

                        product.NameEn = nameEn;

                        if (!string.IsNullOrWhiteSpace(product.Description))
                        {
                            var descEn = await translationService.TranslateAsync(product.Description, "en");
                            bool descStillArabic = System.Text.RegularExpressions.Regex.IsMatch(descEn, @"[\u0600-\u06FF]");
                            product.DescriptionEn = descStillArabic ? null : descEn;
                        }

                        product.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Products.Update(product);
                        translated++;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                // Save each batch to DB
                if (translated > 0 || failed > 0)
                {
                    await _unitOfWork.CompleteAsync();
                }

                // Small delay between batches to avoid API rate limiting
                if (i + batchSize < products.Count)
                {
                    await Task.Delay(300);
                }
            }

            return (translated, skipped, failed);
        }
    }
}