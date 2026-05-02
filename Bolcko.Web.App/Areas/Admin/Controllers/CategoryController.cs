using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoryController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public CategoryController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(categories);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(new CategoryDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryDto categoryDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.CategoryService.AddCategoryAsync(categoryDto);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(categoryDto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _serviceManager.CategoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();
            
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryDto categoryDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.CategoryService.UpdateCategoryAsync(categoryDto);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(categoryDto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.CategoryService.DeleteCategoryAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
