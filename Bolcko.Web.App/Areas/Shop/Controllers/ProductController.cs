using Microsoft.AspNetCore.Mvc;

using Blocko.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
                // Fallback for MVP if no DB data yet, just return the view with some static logic
                return View();
            }
            return View(product);
        }

        public async Task<IActionResult> Search(string query)
        {
            var products = await _serviceManager.ProductService.GetAllProductsAsync();
            if (!string.IsNullOrEmpty(query))
            {
                products = products.Where(p => 
                    (p.Name != null && p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) || 
                    (p.Description != null && p.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }
            
            ViewBag.Query = query;
            return View(products);
        }
    }
}
