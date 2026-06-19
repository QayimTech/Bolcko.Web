using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Web.App.Areas.Admin.Models.ViewModels;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class CategoryController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public CategoryController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var categories = await _serviceManager.CategoryService.GetPagedCategoriesAsync(page, pageSize);
            var viewModel = new CategoryIndexViewModel
            {
                Categories = categories
            };
            return View(viewModel);
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
                try 
                {
                    await _serviceManager.CategoryService.UpdateCategoryAsync(categoryDto);
                    TempData["SuccessMessage"] = "تم تحديث الفئة بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء التحديث: " + ex.Message;
                }
            }
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(categoryDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _serviceManager.CategoryService.DeleteCategoryAsync(id);
                TempData["SuccessMessage"] = "تم حذف الفئة بنجاح.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذه الفئة لوجود منتجات أو فئات فرعية تابعة لها، الرجاء حذفها أولاً.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
