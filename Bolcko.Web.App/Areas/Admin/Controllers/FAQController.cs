using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Content;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class FAQController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public FAQController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index()
        {
            var faqs = await _serviceManager.FAQService.GetAllFAQsAsync();
            return View(faqs);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new FAQItem { IsActive = true, DisplayOrder = 1, PageTarget = "Calculator" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FAQItem item)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.FAQService.CreateFAQAsync(item);
                TempData["Success"] = "تم إضافة السؤال الشائع بنجاح!";
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _serviceManager.FAQService.GetFAQByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FAQItem item)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.FAQService.UpdateFAQAsync(item);
                TempData["Success"] = "تم تحديث السؤال الشائع بنجاح!";
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.FAQService.DeleteFAQAsync(id);
            TempData["Success"] = "تم حذف السؤال الشائع بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            await _serviceManager.FAQService.ToggleActiveAsync(id);
            return Json(new { success = true });
        }
    }
}