using Microsoft.AspNetCore.Mvc;
using Bolcko.Domain.Entities.Tender.DTOs;
using Blocko.Services.Interfaces.Tender;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class QuoteController : Controller
    {
        private readonly ITenderService _tenderService;

        public QuoteController(ITenderService tenderService)
        {
            _tenderService = tenderService;
        }

        [HttpGet]
        public IActionResult Request()
        {
            return View(new QuoteRequestDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(QuoteRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            int? userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "1") : 1;
            await _tenderService.CreateQuoteRequestAsync(dto, userId);

            return RedirectToAction(nameof(Confirmation));
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}