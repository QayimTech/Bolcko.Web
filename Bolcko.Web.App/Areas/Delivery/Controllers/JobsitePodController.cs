using System;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Financing.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Delivery.Controllers
{
    [Area(Delivery)]
    [Route(Delivery/Jobsite)]
    [Route(Delivery/[controller]/[action])]
    public class JobsitePodController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public JobsitePodController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// صفحة توثيق السائق لتسليم الموقع (Geotagged POD)
        /// </summary>
        [HttpGet]
        [Route(POD/{id})]
        [Route(DriverPOD/{id})]
        public async Task<IActionResult> DriverPOD(int id)
        {
            var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();

            // Render the dedicated POD view
            return View(~/Areas/Shop/Views/Financing/DriverPOD.cshtml, tender);
        }

        /// <summary>
        /// رفع إثبات التسليم الموقعي بالـ GPS وفحص الـ Geofencing
        /// </summary>
        [HttpPost]
        [Route(SubmitPOD)]
        [Authorize(Roles = DeliveryDriver, DeliveryCompanyUser, Admin, SuperAdmin)]
        public async Task<IActionResult> SubmitPOD([FromBody] SubmitJobsitePodRequestDto request)
        {
            if (request == null || request.TenderId <= 0)
            {
                return Json(new { success = false, message = بيانات التوصيل غير مكتملة. });
            }

            try
            {
                var pod = await _serviceManager.FinancingService.SubmitJobsitePodAsync(request);
                return Json(new
                {
                    success = true,
                    isWithinGeoFence = pod.IsWithinGeoFence,
                    varianceMeters = pod.DistanceVarianceMeters,
                    message = pod.IsWithinGeoFence 
                        ? تم التحقق من التسليم الجغرافي بنجاح! تم نقل الضمان وتحرير مستحقات التوريد. 
                        : $تم استلام إشعار التوصيل (الانحراف: {pod.DistanceVarianceMeters}م) وتم إرساله للإشراف.
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
