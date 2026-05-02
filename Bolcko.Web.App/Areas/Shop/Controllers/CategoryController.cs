using Microsoft.AspNetCore.Mvc;

using Blocko.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
            {
                var rootCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
                ViewBag.IsRoot = true;
                return View(rootCategories);
            }
            
            var category = await _serviceManager.CategoryService.GetCategoryByIdAsync(id.Value);
            if (category == null) return NotFound();

            var products = await _serviceManager.ProductService.GetProductsByCategoryAsync(id.Value);
            ViewBag.Category = category;
            ViewBag.IsRoot = false;
            
            return View(products);
        }
    }
}
