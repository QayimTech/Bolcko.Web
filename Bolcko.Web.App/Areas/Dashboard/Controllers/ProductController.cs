using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Enums;

namespace Bolcko.Web.App.Areas.Dashboard.Controllers
{
    [Area("Dashboard")]
    [Authorize(Roles = "DashboardUser,Admin")]
    public class ProductController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public ProductController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _serviceManager.ProductService.GetFeaturedProductsAsync(); // Simplified
            return View(products);
        }

        public async Task<IActionResult> Create()
        {
            var categories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            ViewBag.Categories = categories;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            // Logic to save product via service
            // For now, redirect to index
            return RedirectToAction("Index");
        }
    }
}
