using Blocko.Services.Interfaces.Category;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Controllers.Apis.v1
{
    [AllowAnonymous]
    public class MarketController : BaseApiController
    {
        private readonly IMarketPriceService _marketPriceService;

        public MarketController(IMarketPriceService marketPriceService)
        {
            _marketPriceService = marketPriceService;
        }

        /// <summary>
        /// جلب جميع أسعار مواد البناء الحية المحدثة
        /// </summary>
        [HttpGet("prices")]
        public async Task<IActionResult> GetPrices()
        {
            var prices = await _marketPriceService.GetMarketPricesDtoAsync();
            return OkResponse(prices, "تم جلب أسعار مواد البناء الحية بنجاح");
        }

        /// <summary>
        /// جلب شريط مؤشر البورصة والأسعار المباشرة السريع
        /// </summary>
        [HttpGet("ticker")]
        public async Task<IActionResult> GetLiveTicker()
        {
            var ticker = await _marketPriceService.GetLiveTickerAsync();
            return OkResponse(ticker, "تم جلب مؤشر البورصة الحي بنجاح");
        }

        /// <summary>
        /// حاسبة كميات وتكلفة مواد البناء للمشروع (حديد، خرسانة، إسمنت، طوب، رمل، عمالة)
        /// </summary>
        [HttpPost("calculator/estimate")]
        public async Task<IActionResult> CalculateEstimate([FromBody] ConstructionEstimateRequestDto request)
        {
            if (request == null)
            {
                return ErrorResponse("بيانات الطلب غير صالحة");
            }

            var result = await _marketPriceService.CalculateEstimateAsync(request);
            return OkResponse(result, "تم احتساب كميات وتكلفة المشروع التقديرية بنجاح");
        }

        /// <summary>
        /// تحديث مباشر للأسعار العالمية وتطبيق المعادلات (أدمن فقط)
        /// </summary>
        [HttpPost("sync-live")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ForceSyncLivePrices()
        {
            await _marketPriceService.SyncLiveGlobalMarketPricesAsync();
            var ticker = await _marketPriceService.GetLiveTickerAsync();
            return OkResponse(ticker, "تمت مزامنة وتحديث أسعار السوق اللحظية بنجاح");
        }
    }
}
