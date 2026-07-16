using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Domain.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Extensions
{
    public static class TranslationExtensions
    {
        public static async Task<ProductDto> TranslateAsync(this ProductDto product, ITranslationService translationService, string targetCulture)
        {
            if (product == null) return null!;
            product.Name = await translationService.TranslateAsync(product.Name, targetCulture);
            if (!string.IsNullOrEmpty(product.Description))
            {
                product.Description = await translationService.TranslateAsync(product.Description, targetCulture);
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

        public static async Task<IEnumerable<ProductDto>> TranslateAsync(this IEnumerable<ProductDto> products, ITranslationService translationService, string targetCulture)
        {
            if (products == null) return null!;
            var list = new List<ProductDto>();
            foreach (var p in products)
            {
                list.Add(await p.TranslateAsync(translationService, targetCulture));
            }
            return list;
        }

        public static async Task<IPagedList<ProductDto>> TranslateAsync(this IPagedList<ProductDto> pagedProducts, ITranslationService translationService, string targetCulture)
        {
            if (pagedProducts == null) return null!;
            var translatedItems = await pagedProducts.Items.TranslateAsync(translationService, targetCulture);
            return new Blocko.Persistence.Common.PagedList<ProductDto>(
                translatedItems, 
                pagedProducts.TotalCount, 
                pagedProducts.PageIndex, 
                pagedProducts.PageSize
            );
        }

        public static async Task<CategoryDto> TranslateAsync(this CategoryDto category, ITranslationService translationService, string targetCulture)
        {
            if (category == null) return null!;
            category.Name = await translationService.TranslateAsync(category.Name, targetCulture);
            if (!string.IsNullOrEmpty(category.Description))
            {
                category.Description = await translationService.TranslateAsync(category.Description, targetCulture);
            }
            return category;
        }

        public static async Task<IEnumerable<CategoryDto>> TranslateAsync(this IEnumerable<CategoryDto> categories, ITranslationService translationService, string targetCulture)
        {
            if (categories == null) return null!;
            var list = new List<CategoryDto>();
            foreach (var c in categories)
            {
                list.Add(await c.TranslateAsync(translationService, targetCulture));
            }
            return list;
        }

        public static async Task<Bolcko.Domain.Entities.Catalog.MarketPrice> TranslateAsync(this Bolcko.Domain.Entities.Catalog.MarketPrice price, ITranslationService translationService, string targetCulture)
        {
            if (price == null) return null!;
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
            var list = new List<Bolcko.Domain.Entities.Catalog.MarketPrice>();
            foreach (var p in prices)
            {
                list.Add(await p.TranslateAsync(translationService, targetCulture));
            }
            return list;
        }
    }
}
