using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Web.App.Extensions;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class CategoryController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly ITranslationService _translationService;

        public CategoryController(IServiceManager serviceManager, ITranslationService translationService)
        {
            _serviceManager = serviceManager;
            _translationService = translationService;
        }

        public async Task<IActionResult> Index(int? id, int page = 1, int pageSize = 12)
        {
            var culture = CultureInfo.CurrentCulture.Name;

            if (id == null)
            {
                var rootCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
                var translatedRoots = await rootCategories.TranslateAsync(_translationService, culture);
                
                ViewBag.IsRoot = true;
                return View(translatedRoots);
            }

            var category = await _serviceManager.CategoryService.GetCategoryByIdAsync(id.Value);
            if (category == null) return NotFound();
            await category.TranslateAsync(_translationService, culture);

            var subCategories = await _serviceManager.CategoryService.GetSubCategoriesAsync(id.Value);
            var translatedSubs = await subCategories.TranslateAsync(_translationService, culture);

            var products = await _serviceManager.ProductService.SearchCatalogProductsPagedAsync(null, id.Value, page, pageSize);
            var translatedProducts = await products.TranslateAsync(_translationService, culture);

            ViewBag.Category = category;
            ViewBag.SubCategories = translatedSubs;
            ViewBag.IsRoot = false;

            return View(translatedProducts);
        }
    }
}
