using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog.DTOs;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class CalculatorController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public CalculatorController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        [Route("calculator")]
        [Route("Shop/Calculator")]
        [Route("Shop/Calculator/Index")]
        public async Task<IActionResult> Index()
        {
            var defaultReq = new ConstructionEstimateRequestDto
            {
                BuiltUpAreaSquareMeters = 250,
                NumberOfFloors = 2,
                BuildingType = "Residential",
                FoundationType = "IsolatedFootings",
                City = "Amman"
            };

            var estimate = await _serviceManager.MarketPriceService.CalculateEstimateAsync(defaultReq);
            ViewBag.Prices = await _serviceManager.MarketPriceService.GetMarketPricesDtoAsync();
            ViewBag.FAQs = await _serviceManager.FAQService.GetActiveFAQsByPageAsync("Calculator");

            return View(estimate);
        }
    }
}