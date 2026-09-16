using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Financing.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Route("Shop/[controller]")]
    public class FinancingController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public FinancingController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// بوابة المستثمر لعطاءات المرابحة الإنشائية (Investor Murabaha Portal)
        /// </summary>
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var tenders = await _serviceManager.FinancingService.GetOpenTendersAsync();
            return View(tenders);
        }

        /// <summary>
        /// تفاصيل عطاء التمويل مع كشف الكميات ورخصة المشروع
        /// </summary>
        [HttpGet]
        [Route("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            if (tender == null)
            {
                return NotFound();
            }
            return View(tender);
        }

        /// <summary>
        /// تحويل سلة حسابات الـ BOQ إلى عطاء مرابحة إسلامية
        /// </summary>
        [HttpPost]
        [Route("CreateFromBOQ")]
        public async Task<IActionResult> CreateFromBOQ([FromBody] CreateFinancingTenderRequestDto request)
        {
            if (request == null || !request.Items.Any() || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
            {
                return Json(new { success = false, message = "يرجى إدخال اسم المشروع والاسم ورقم الهاتف والكميات للمتابعة." });
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) userId = id;
            }

            var tender = await _serviceManager.FinancingService.CreateTenderFromBOQAsync(request, userId);

            return Json(new
            {
                success = true,
                trackingCode = tender.TrackingCode,
                tenderId = tender.Id,
                totalPayable = tender.TotalPayableAmount,
                tenureDays = tender.TenureDays,
                message = "تم طرح عطاء تمويل المرابحة بنجاح! سيتم إشعار المستثمرين التمويليين للشراء والتمليك فوراً."
            });
        }

        /// <summary>
        /// موافقة وتمويل المستثمر للعطاء بنظام الوكالة الشرعية
        /// </summary>
        [HttpPost]
        [Route("Fund")]
        public async Task<IActionResult> Fund([FromBody] FundTenderRequestDto request)
        {
            if (request == null || request.TenderId <= 0 || string.IsNullOrWhiteSpace(request.FunderName) || string.IsNullOrWhiteSpace(request.FunderPhone))
            {
                return Json(new { success = false, message = "يرجى إدخال الاسم ورقم الهاتف والموافقة على شروط عقد الوكالة بالمرابحة." });
            }

            int? funderId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) funderId = id;
            }

            var result = await _serviceManager.FinancingService.FundTenderAsync(request, funderId);
            if (!result)
            {
                return Json(new { success = false, message = "تعذر تمويل العطاء، قد يكون العطاء ممولاً مسبقاً أو غير متاح." });
            }

            return Json(new
            {
                success = true,
                message = "تم تمويل العطاء وشراء البضاعة بنجاح! تم إصدار أمر الشراء (PO) وتوليد عقد الوكالة الشرعي."
            });
        }

        /// <summary>
        /// واجهة السائق لتوثيق التسليم الموقعي بالـ GPS والـ Camera (Mobile Web POD)
        /// </summary>
        [HttpGet]
        [Route("DriverPOD/{id}")]
        public async Task<IActionResult> DriverPOD(int id)
        {
            var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            if (tender == null) return NotFound();
            return View(tender);
        }

        /// <summary>
        /// رفع إثبات التسليم الموقعي وفحص الـ Geofencing
        /// </summary>
        [HttpPost]
        [Route("SubmitPOD")]
        public async Task<IActionResult> SubmitPOD([FromBody] SubmitJobsitePodRequestDto request)
        {
            if (request == null || request.TenderId <= 0)
            {
                return Json(new { success = false, message = "بيانات التوصيل غير مكتملة." });
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
                        ? "تم التحقق من التسليم الجغرافي بنجاح! تم نقل الضمان وتحرير مستحقات التوريد." 
                        : $"تم استلام إشعار التوصيل (الانحراف: {pod.DistanceVarianceMeters}م) وتم إرساله للإشراف."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// لوحة تحكم المقاول (Contractor Dashboard) - مؤشر الموثوقية Trust Score، سقف الائتمان، والأقساط
        /// </summary>
        [HttpGet]
        [Route("ContractorDashboard")]
        public async Task<IActionResult> ContractorDashboard()
        {
            int? userId = null;
            string? phone = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) userId = id;
            }

            var dashboard = await _serviceManager.FinancingService.GetContractorDashboardAsync(userId, phone);
            return View(dashboard);
        }

        /// <summary>
        /// لوحة تحكم المستثمر (Investor Portfolio Dashboard) - المحفظة، العوائد الصافية، والعقود
        /// </summary>
        [HttpGet]
        [Route("InvestorDashboard")]
        [Route("Portfolio")]
        public async Task<IActionResult> InvestorDashboard()
        {
            int? userId = null;
            string? phone = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) userId = id;
            }

            var dashboard = await _serviceManager.FinancingService.GetInvestorDashboardAsync(userId, phone);
            return View(dashboard);
        }

        /// <summary>
        /// سداد وتسوية عطاء المرابحة (Settlement Engine)
        /// </summary>
        [HttpPost]
        [Route("Settle/{id}")]
        public async Task<IActionResult> Settle(int id)
        {
            var success = await _serviceManager.FinancingService.SettleTenderAsync(id);
            if (!success)
            {
                return Json(new { success = false, message = "تعذر إتمام التسوية. يرجى التحقق من حالة العطاء." });
            }

            return Json(new
            {
                success = true,
                message = "تمت التسوية المالية بنجاح! تم توزيع الأرباح على المستثمر واقتطاع أجر الوكالة ورفع تقييم موثوقية المقاول."
            });
        }
    }
}