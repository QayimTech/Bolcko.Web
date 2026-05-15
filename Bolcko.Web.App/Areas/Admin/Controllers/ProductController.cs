using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Web.App.Areas.Admin.Models.ViewModels;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class ProductController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public ProductController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var products = await _serviceManager.ProductService.GetPagedProductsAsync(page, pageSize);
            var viewModel = new ProductIndexViewModel
            {
                Products = products
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(new ProductDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDto productDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.ProductService.AddProductAsync(productDto);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(productDto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _serviceManager.ProductService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductDto productDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.ProductService.UpdateProductAsync(productDto);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(productDto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.ProductService.DeleteProductAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
