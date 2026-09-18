using Blocko.Services.Interfaces;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Delivery.Controllers
{
    [Area(Delivery)]
    [Route(delivery/status)]
    [Route(Delivery/Portal/[action])]
    public class DeliveryPortalController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public DeliveryPortalController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet({token})]
        public async Task<IActionResult> Index(string token)
        {
            if (string.IsNullOrEmpty(token)) return NotFound(رابط التوصيل غير صالح أو منتهي الصلاحية.);
            var job = await _serviceManager.DeliveryService.GetJobByTokenAsync(token);
            if (job == null)
            {
                return NotFound(رابط التوصيل غير صالح أو منتهي الصلاحية.);
            }

            return View(~/Views/DeliveryPortal/Index.cshtml, job);
        }

        [HttpPost(update/{token})]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string token, DeliveryJobStatus status)
        {
            var job = await _serviceManager.DeliveryService.GetJobByTokenAsync(token);
            if (job == null)
            {
                return NotFound(رابط التوصيل غير صالح أو منتهي الصلاحية.);
            }

            try
            {
                await _serviceManager.DeliveryService.UpdateJobStatusAsync(job.Id, status);
                TempData[SuccessMessage] = تم تحديث حالة الطلب بنجاح.;
            }
            catch (Exception ex)
            {
                TempData[ErrorMessage] = $حدث خطأ أثناء التحديث: {ex.Message};
            }

            return RedirectToAction(Index, new { token });
        }
    }
}
