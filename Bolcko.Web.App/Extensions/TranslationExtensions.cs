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
                // English culture: prefer stored NameEn/DescriptionEn, fallback to API only if missing
                if (!string.IsNullOrEmpty(product.NameEn))
                {
                    product.Name = product.NameEn;
                    if (!string.IsNullOrEmpty(product.DescriptionEn))
                    {
                        product.Description = product.DescriptionEn;
                    }
                    // Translate CategoryName and UnitOfMeasure only if they contain Arabic characters
                    if (!string.IsNullOrEmpty(product.CategoryName) && ContainsArabic(product.CategoryName))
                        product.CategoryName = await translationService.TranslateAsync(product.CategoryName, targetCulture);
                    if (!string.IsNullOrEmpty(product.UnitOfMeasure) && ContainsArabic(product.UnitOfMeasure))
                        product.UnitOfMeasure = await translationService.TranslateAsync(product.UnitOfMeasure, targetCulture);
                }
                else
                {
                    product.Name = await translationService.TranslateAsync(product.Name, targetCulture);
                    if (!string.IsNullOrEmpty(product.Description))
                        product.Description = await translationService.TranslateAsync(product.Description, targetCulture);
                    if (!string.IsNullOrEmpty(product.CategoryName))
                        product.CategoryName = await translationService.TranslateAsync(product.CategoryName, targetCulture);
                    if (!string.IsNullOrEmpty(product.UnitOfMeasure))
                        product.UnitOfMeasure = await translationService.TranslateAsync(product.UnitOfMeasure, targetCulture);
                }
            }
            else
            {
                // Arabic culture: DB values are already in Arabic - skip API calls completely
                // TranslationService already has an Arabic short-circuit, but this avoids even the cache lookup overhead
                // Only call translate if text is NOT already Arabic (edge case: English product in Arabic store)
                if (!string.IsNullOrEmpty(product.Name) && !ContainsArabic(product.Name))
                    product.Name = await translationService.TranslateAsync(product.Name, targetCulture);
                if (!string.IsNullOrEmpty(product.Description) && !ContainsArabic(product.Description))
                    product.Description = await translationService.TranslateAsync(product.Description, targetCulture);
                // CategoryName and UnitOfMeasure are almost always Arabic in DB - skip
            }

            return product;
        }

        private static bool ContainsArabic(string text) =>
            System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0600-\u06FF]");

        public static async Task<IEnumerable<ProductDto>> TranslateAsync(this IEnumerable<ProductDto> products, ITranslationService translationService, string targetCulture, IUnitOfWork? unitOfWork = null)
        {
            if (products == null) return null!;
            var tasks = new List<Task<ProductDto>>();
            foreach (var p in products)
            {
                tasks.Add(p.TranslateAsync(translationService, targetCulture, null)); // Force null unitOfWork to prevent parallel DB writes
            }
            return await Task.WhenAll(tasks);
        }

        public static async Task<IPagedList<ProductDto>> TranslateAsync(this IPagedList<ProductDto> pagedProducts, ITranslationService translationService, string targetCulture, IUnitOfWork? unitOfWork = null)
        {
            if (pagedProducts == null) return null!;
            var translatedItems = await pagedProducts.Items.TranslateAsync(translationService, targetCulture, null); // Force null unitOfWork
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
