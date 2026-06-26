using Microsoft.AspNetCore.Mvc;

using Blocko.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Domain.Entities.Catalog.DTOs;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class CategoryController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public CategoryController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index(int? id, int page = 1, int pageSize = 12)
        {
            if (id == null)
            {
                var rootCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
                ViewBag.IsRoot = true;
                return View(rootCategories);
            }

            var category = await _serviceManager.CategoryService.GetCategoryByIdAsync(id.Value);
            if (category == null) return NotFound();

            var subCategories = await _serviceManager.CategoryService.GetSubCategoriesAsync(id.Value);
            var products = await _serviceManager.ProductService.SearchCatalogProductsPagedAsync(null, id.Value, page, pageSize);

            ViewBag.Category = category;
            ViewBag.SubCategories = subCategories;
            ViewBag.IsRoot = false;

            return View(products);
        }

    }
}
