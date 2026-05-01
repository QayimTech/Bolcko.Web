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
                var categories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
                return View(categories);
            }
            
            var products = await _serviceManager.ProductService.GetProductsByCategoryAsync(id.Value);
            return View(products);
        }
    }
}
