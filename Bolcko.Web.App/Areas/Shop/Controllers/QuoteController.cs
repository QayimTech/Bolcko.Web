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
        [ActionName("Request")]
        public IActionResult RequestGet([FromQuery] QuoteRequestDto? dto)
        {
            return View("Request", dto ?? new QuoteRequestDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Request(QuoteRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            int? userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "") : null;
            var result = await _tenderService.CreateQuoteRequestAsync(dto, userId);

            return RedirectToAction(nameof(Confirmation), new { id = result.Id });
        }

        public IActionResult Confirmation(int? id)
        {
            ViewBag.TenderId = id;
            return View();
        }
    }
}