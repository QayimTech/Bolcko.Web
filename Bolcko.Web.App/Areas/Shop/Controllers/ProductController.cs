using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class ProductController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public ProductController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index(int id)
        {
            var product = await _serviceManager.ProductService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        public async Task<IActionResult> Search(string query)
        {
            var products = await _serviceManager.ProductService.SearchProductsAsync(query);
            ViewBag.Query = query;
            return View(products);
        }
    }
}
