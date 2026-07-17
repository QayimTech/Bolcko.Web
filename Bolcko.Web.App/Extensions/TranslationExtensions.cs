using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Domain.Common;
using Bolcko.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace Bolcko.Web.App.Extensions
{
    public static class TranslationExtensions
    {
        public static async Task<ProductDto> TranslateAsync(this ProductDto product, ITranslationService translationService, string targetCulture, IUnitOfWork? unitOfWork = null)
        {
            if (product == null) return null!;
            
            var isAr = targetCulture.StartsWith("ar");
            if (!isAr)
            {
                if (!string.IsNullOrEmpty(product.NameEn))
                {
                    product.Name = product.NameEn;
                    if (!string.IsNullOrEmpty(product.DescriptionEn))
                    {
                        product.Description = product.DescriptionEn;
                    }
                }
                else
                {
                    product.Name = await translationService.TranslateAsync(product.Name, targetCulture);
                    if (!string.IsNullOrEmpty(product.Description))
                    {
                        product.Description = await translationService.TranslateAsync(product.Description, targetCulture);
                    }
                    
                    if (unitOfWork != null)
                    {
                        try
                        {
                            var entity = await unitOfWork.Products.GetByIdAsync(product.Id);
                            if (entity != null)
                            {
                                entity.NameEn = product.Name;
                                entity.DescriptionEn = product.Description;
                                unitOfWork.Products.Update(entity);
                                await unitOfWork.SaveChangesAsync();
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            else
            {
                product.Name = await translationService.TranslateAsync(product.Name, targetCulture);
                if (!string.IsNullOrEmpty(product.Description))
                {
                    product.Description = await translationService.TranslateAsync(product.Description, targetCulture);
                }
            }

            if (!string.IsNullOrEmpty(product.CategoryName))
            {
                product.CategoryName = await translationService.TranslateAsync(product.CategoryName, targetCulture);
            }
            if (!string.IsNullOrEmpty(product.UnitOfMeasure))
            {
                product.UnitOfMeasure = await translationService.TranslateAsync(product.UnitOfMeasure, targetCulture);
            }
            return product;
        }

        public static async Task<IEnumerable<ProductDto>> TranslateAsync(this IEnumerable<ProductDto> products, ITranslationService translationService, string targetCulture, IUnitOfWork? unitOfWork = null)
        {
            if (products == null) return null!;
            var tasks = new List<Task<ProductDto>>();
            foreach (var p in products)
            {
                tasks.Add(p.TranslateAsync(translationService, targetCulture, unitOfWork));
            }
            return await Task.WhenAll(tasks);
        }

        public static async Task<IPagedList<ProductDto>> TranslateAsync(this IPagedList<ProductDto> pagedProducts, ITranslationService translationService, string targetCulture, IUnitOfWork? unitOfWork = null)
        {
            if (pagedProducts == null) return null!;
            var translatedItems = await pagedProducts.Items.TranslateAsync(translationService, targetCulture, unitOfWork);
            return new Blocko.Persistence.Common.PagedList<ProductDto>(
                translatedItems, 
                pagedProducts.TotalCount, 
                pagedProducts.PageIndex, 
                pagedProducts.PageSize
            );
        }

        public static async Task<CategoryDto> TranslateAsync(this CategoryDto category, ITranslationService translationService, string targetCulture, IUnitOfWork? unitOfWork = null)
        {
            if (category == null) return null!;
            var isAr = targetCulture.StartsWith("ar");
            if (!isAr)
            {
                if (!string.IsNullOrEmpty(category.NameEn))
                {
                    category.Name = category.NameEn;
                    if (!string.IsNullOrEmpty(category.DescriptionEn))
                    {
                        category.Description = category.DescriptionEn;
                    }
                }
                else
                {
                    category.Name = await translationService.TranslateAsync(category.Name, targetCulture);
                    if (!string.IsNullOrEmpty(category.Description))
                    {
                        category.Description = await translationService.TranslateAsync(category.Description, targetCulture);
                    }
                    
                    if (unitOfWork != null)
                    {
                        try
                        {
                            var entity = await unitOfWork.Categories.GetByIdAsync(category.Id);
                            if (entity != null)
                            {
                                entity.NameEn = category.Name;
                                entity.DescriptionEn = category.Description;
                                unitOfWork.Categories.Update(entity);
                                await unitOfWork.SaveChangesAsync();
                            }
                        }
                        catch
                        {
                        }
                    }
                }
            }
            else
            {
                category.Name = await translationService.TranslateAsync(category.Name, targetCulture);
                if (!string.IsNullOrEmpty(category.Description))
                {
                    category.Description = await translationService.TranslateAsync(category.Description, targetCulture);
                }
            }
            return category;
        }

        public static async Task<IEnumerable<CategoryDto>> TranslateAsync(this IEnumerable<CategoryDto> categories, ITranslationService translationService, string targetCulture, IUnitOfWork? unitOfWork = null)
        {
            if (categories == null) return null!;
            var tasks = new List<Task<CategoryDto>>();
            foreach (var c in categories)
            {
                tasks.Add(c.TranslateAsync(translationService, targetCulture, unitOfWork));
            }
            return await Task.WhenAll(tasks);
        }

        public static async Task<Bolcko.Domain.Entities.Catalog.MarketPrice> TranslateAsync(this Bolcko.Domain.Entities.Catalog.MarketPrice price, ITranslationService translationService, string targetCulture)
        {
            if (price == null) return null!;
            
            // Bypass translation completely if target is Arabic, since database values are already in Arabic
            if (targetCulture.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            {
                return price;
            }

            price.MaterialName = await translationService.TranslateAsync(price.MaterialName, targetCulture);
            if (!string.IsNullOrEmpty(price.UnitOfMeasure))
            {
                price.UnitOfMeasure = await translationService.TranslateAsync(price.UnitOfMeasure, targetCulture);
            }
            if (!string.IsNullOrEmpty(price.Currency))
            {
                price.Currency = await translationService.TranslateAsync(price.Currency, targetCulture);
            }
            return price;
        }

        public static async Task<IEnumerable<Bolcko.Domain.Entities.Catalog.MarketPrice>> TranslateAsync(this IEnumerable<Bolcko.Domain.Entities.Catalog.MarketPrice> prices, ITranslationService translationService, string targetCulture)
        {
            if (prices == null) return null!;
            var tasks = new List<Task<Bolcko.Domain.Entities.Catalog.MarketPrice>>();
            foreach (var p in prices)
            {
                tasks.Add(p.TranslateAsync(translationService, targetCulture));
            }
            return await Task.WhenAll(tasks);
        }
    }
}
