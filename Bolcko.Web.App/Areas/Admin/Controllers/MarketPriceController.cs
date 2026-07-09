using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class MarketPriceController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public MarketPriceController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // GET: Admin/MarketPrice
        public async Task<IActionResult> Index()
        {
            var prices = await _serviceManager.MarketPriceService.GetAllMarketPricesAsync();
            return View(prices);
        }

        // GET: Admin/MarketPrice/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var price = await _serviceManager.MarketPriceService.GetMarketPriceByIdAsync(id);
            if (price == null)
            {
                return NotFound();
            }
            return View(price);
        }

        // POST: Admin/MarketPrice/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MarketPrice model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                await _serviceManager.MarketPriceService.UpdateMarketPriceAsync(model);
                TempData["SuccessMessage"] = "تم تحديث سعر المادة بنجاح!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
