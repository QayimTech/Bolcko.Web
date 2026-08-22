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

        public static async Task<IEnumerable<ProductDto>> TranslateAsync(
            this IEnumerable<ProductDto> products, 
            ITranslationService translationService, 
            string targetCulture, 
            IUnitOfWork? unitOfWork = null, 
            IServiceProvider? serviceProvider = null)
        {
            if (products == null) return null!;
            
            Serilog.Log.Information("[TranslationExtensions] TranslateAsync started for {Count} products. Culture: {Culture}. HasServiceProvider: {HasSP}, HasUOW: {HasUOW}", 
                products.Count(), targetCulture, serviceProvider != null, unitOfWork != null);

            var tasks = new List<Task<ProductDto>>();
            foreach (var p in products)
            {
                tasks.Add(p.TranslateAsync(translationService, targetCulture, null));
            }
            
            var results = await Task.WhenAll(tasks);
            
            // Background DB Persistence for English Translations:
            if (!targetCulture.StartsWith("ar", StringComparison.OrdinalIgnoreCase))
            {
                var productsToSave = new List<ProductDto>();
                foreach (var p in results)
                {
                    if (string.IsNullOrEmpty(p.NameEn) && !string.IsNullOrEmpty(p.Name))
                    {
                        productsToSave.Add(p);
                    }
                }

                Serilog.Log.Information("[TranslationExtensions] Found {Count} products needing DB persistence for English translation.", productsToSave.Count);

                if (productsToSave.Count > 0)
                {
                    if (serviceProvider != null)
                    {
                        Serilog.Log.Information("[TranslationExtensions] Spawning background task to save translations for products: {ProductIds}", 
                            string.Join(", ", productsToSave.Select(p => p.Id)));

                        var scopeFactory = (Microsoft.Extensions.DependencyInjection.IServiceScopeFactory)serviceProvider.GetService(typeof(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory))!;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using (var scope = scopeFactory.CreateScope())
                                {
                                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                                    int updatedCount = 0;
                                    foreach (var pDto in productsToSave)
                                    {
                                        var productEntity = await uow.Products.GetByIdAsync(pDto.Id);
                                        if (productEntity != null && string.IsNullOrEmpty(productEntity.NameEn))
                                        {
                                            productEntity.NameEn = pDto.Name;
                                            productEntity.DescriptionEn = pDto.Description;
                                            uow.Products.Update(productEntity);
                                            updatedCount++;
                                        }
                                    }
                                    if (updatedCount > 0)
                                    {
                                        await uow.CompleteAsync();
                                        Serilog.Log.Information("[TranslationExtensions] Background task successfully saved {Count} translations to DB.", updatedCount);
                                    }
                                    else
                                    {
                                        Serilog.Log.Warning("[TranslationExtensions] Background task completed but no database entities were updated.");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Serilog.Log.Error(ex, "[TranslationExtensions] Exception in background translation DB writer.");
                            }
                        });
                    }
                    else if (unitOfWork != null)
                    {
                        try
                        {
                            int updatedCount = 0;
                            foreach (var pDto in productsToSave)
                            {
                                var productEntity = await unitOfWork.Products.GetByIdAsync(pDto.Id);
                                if (productEntity != null && string.IsNullOrEmpty(productEntity.NameEn))
                                {
                                    productEntity.NameEn = pDto.Name;
                                    productEntity.DescriptionEn = pDto.Description;
                                    unitOfWork.Products.Update(productEntity);
                                    updatedCount++;
                                }
                            }
                            if (updatedCount > 0)
                            {
                                await unitOfWork.CompleteAsync();
                                Serilog.Log.Information("[TranslationExtensions] Sequential task successfully saved {Count} translations to DB.", updatedCount);
                            }
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Error(ex, "[TranslationExtensions] Exception in sequential translation DB writer.");
                        }
                    }
                }
            }

            return results;
        }

        // Clean overload for Background ServiceProvider persistence
        public static Task<IEnumerable<ProductDto>> TranslateAsync(
            this IEnumerable<ProductDto> products, 
            ITranslationService translationService, 
            string targetCulture, 
            IServiceProvider serviceProvider) =>
            TranslateAsync(products, translationService, targetCulture, null, serviceProvider);

        public static async Task<IPagedList<ProductDto>> TranslateAsync(
            this IPagedList<ProductDto> pagedProducts, 
            ITranslationService translationService, 
            string targetCulture, 
            IUnitOfWork? unitOfWork = null, 
            IServiceProvider? serviceProvider = null)
        {
            if (pagedProducts == null) return null!;
            var translatedItems = await pagedProducts.Items.TranslateAsync(translationService, targetCulture, unitOfWork, serviceProvider);
            return new Blocko.Persistence.Common.PagedList<ProductDto>(
                translatedItems, 
                pagedProducts.TotalCount, 
                pagedProducts.PageIndex, 
                pagedProducts.PageSize
            );
        }

        public static async Task<CategoryDto> TranslateAsync(
            this CategoryDto category, 
            ITranslationService translationService, 
            string targetCulture, 
            IUnitOfWork? unitOfWork = null, 
            IServiceProvider? serviceProvider = null)
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
                    
                    // Persist category translation to database
                    if (unitOfWork != null)
                    {
                        try
                        {
                            var categoryEntity = await unitOfWork.Categories.GetByIdAsync(category.Id);
                            if (categoryEntity != null && string.IsNullOrEmpty(categoryEntity.NameEn))
                            {
                                categoryEntity.NameEn = category.Name;
                                categoryEntity.DescriptionEn = category.Description;
                                unitOfWork.Categories.Update(categoryEntity);
                                await unitOfWork.CompleteAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CategoryTranslationSave] Error: {ex.Message}");
                        }
                    }
                }

                // Parent category name translation for English
                if (!string.IsNullOrEmpty(category.ParentCategoryNameEn))
                {
                    category.ParentCategoryName = category.ParentCategoryNameEn;
                }
                else if (!string.IsNullOrEmpty(category.ParentCategoryName) && ContainsArabic(category.ParentCategoryName))
                {
                    category.ParentCategoryName = await translationService.TranslateAsync(category.ParentCategoryName, targetCulture);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(category.Name) && !ContainsArabic(category.Name))
                    category.Name = await translationService.TranslateAsync(category.Name, targetCulture);
                if (!string.IsNullOrEmpty(category.Description) && !ContainsArabic(category.Description))
                    category.Description = await translationService.TranslateAsync(category.Description, targetCulture);
            }
            return category;
        }

        public static async Task<IEnumerable<CategoryDto>> TranslateAsync(
            this IEnumerable<CategoryDto> categories, 
            ITranslationService translationService, 
            string targetCulture, 
            IUnitOfWork? unitOfWork = null, 
            IServiceProvider? serviceProvider = null)
        {
            if (categories == null) return null!;
            var tasks = new List<Task<CategoryDto>>();
            foreach (var c in categories)
            {
                tasks.Add(c.TranslateAsync(translationService, targetCulture, unitOfWork, serviceProvider));
            }
            return await Task.WhenAll(tasks);
        }

        // Clean overload for Background ServiceProvider persistence
        public static Task<IEnumerable<CategoryDto>> TranslateAsync(
            this IEnumerable<CategoryDto> categories, 
            ITranslationService translationService, 
            string targetCulture, 
            IServiceProvider serviceProvider) =>
            TranslateAsync(categories, translationService, targetCulture, null, serviceProvider);

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
