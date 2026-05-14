using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.SEO.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]

    public class SEOController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public SEOController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index()
        {
            var seoList = await _serviceManager.SEOService.GetAllSEOAsync();
            return View(seoList);
        }

        public async Task<IActionResult> Create()
        {
            return View(new SEOMetadataDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SEOMetadataDto seoDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.SEOService.AddOrUpdateSEOAsync(seoDto);
                return RedirectToAction(nameof(Index));
            }
            return View(seoDto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var seo = await _serviceManager.SEOService.GetSEOByIdAsync(id);
            if (seo == null) return NotFound();
            return View(seo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SEOMetadataDto seoDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.SEOService.AddOrUpdateSEOAsync(seoDto);
                return RedirectToAction(nameof(Index));
            }
            return View(seoDto);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.SEOService.DeleteSEOAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
