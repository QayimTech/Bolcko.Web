using System;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Delivery.DTOs;
using Bolcko.Domain.Entities.Financing.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Delivery.Controllers
{
    [Area("Delivery")]
    [Route("Delivery/Jobsite")]
    [Route("Delivery/[controller]/[action]")]
    public class JobsitePodController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public JobsitePodController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // ==========================================
        // LOG-06: Jobsite e-POD Cockpit & Verification
        // ==========================================
        [HttpGet]
        [Route("Verify/{id}")]
        [Route("JobsiteVerification/{id}")]
        public async Task<IActionResult> Verify(int id)
        {
            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(id);
            if (job == null)
            {
                job = await _serviceManager.DeliveryService.GetJobByOrderIdAsync(id);
            }

            if (job != null)
            {
                var txn = await _serviceManager.DeliveryService.GetPayoutTransactionByJobIdAsync(job.Id);
                ViewBag.PayoutTransaction = txn;
                return View("~/Areas/Delivery/Views/JobsitePod/Verify.cshtml", job);
            }

            // Fallback for financing tender legacy route
            var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            if (tender != null)
            {
                return View("~/Areas/Shop/Views/Financing/DriverPOD.cshtml", tender);
            }

            return NotFound("لم يتم العثور على أمر التوصيل المطلوب.");
        }

        [HttpPost]
        [Route("ReleaseOtpPayout")]
        public async Task<IActionResult> ReleaseOtpPayout([FromBody] CarrierPayoutRequestDto request)
        {
            if (request == null || request.JobId <= 0 || string.IsNullOrWhiteSpace(request.OtpCode))
            {
                return Json(new { success = false, message = "يرجى إدخال رمز التحقق الميداني (OTP) المكون من 6 أرقام." });
            }

            try
            {
                var result = await _serviceManager.DeliveryService.ReleaseJobsiteOtpPayoutAsync(request.JobId, request.OtpCode, request.ReceiverNotes);
                return Json(new
                {
                    success = result.Success,
                    message = result.Message,
                    payoutAmount = result.PayoutAmountJod,
                    grossAmount = result.GrossFreightAmount,
                    takeRate = result.PlatformTakeRate,
                    cliqAlias = result.CliqAlias,
                    transactionReference = result.TransactionReference,
                    epodDocumentUrl = result.EpodDocumentUrl,
                    isEscrowReleased = result.IsEscrowReleased,
                    settledAt = result.SettledAt.ToString("yyyy/MM/dd HH:mm")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطأ في معالجة الإفراج المالي: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("RaiseDispute")]
        public async Task<IActionResult> RaiseDispute(int jobId, string reason, string? photoUrl)
        {
            if (jobId <= 0 || string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { success = false, message = "يرجى توضيح سبب النزاع أو الاعتراض." });
            }

            try
            {
                var ok = await _serviceManager.DeliveryService.DisputeDeliveryJobAsync(jobId, reason, photoUrl);
                return Json(new
                {
                    success = ok,
                    message = "تم تجميد الضمان المالي للشحنة وإحالة الاعتراض إلى فريق الرقابة والمطابقة الميدانية."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"تعذر تسجيل النزاع: {ex.Message}" });
            }
        }

        // ==========================================
        // Financing Tender e-POD Legacy Endpoints
        // ==========================================
        [HttpGet]
        [Route("POD/{id}")]
        [Route("DriverPOD/{id}")]
        public async Task<IActionResult> DriverPOD(int id)
        {
            var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            if (tender != null)
            {
                return View("~/Areas/Shop/Views/Financing/DriverPOD.cshtml", tender);
            }

            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(id);
            if (job != null)
            {
                return RedirectToAction("Verify", new { id = job.Id });
            }

            return NotFound();
        }

        [HttpPost]
        [Route("GenerateOTP/{id}")]
        public async Task<IActionResult> GenerateOTP(int id)
        {
            try
            {
                var otp = await _serviceManager.FinancingService.GenerateDeliveryOtpAsync(id);
                return Json(new { success = true, otp = otp, message = "تم توليد رمز OTP بنجاح." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Route("SubmitPOD")]
        [AllowAnonymous]
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
                        ? "تم التحقق من رمز الـ OTP والتسليم الجغرافي بنجاح! تم تحرير أموال الضمان (Escrow) للمورد وتفعيل المرابحة." 
                        : $"تم التحقق من الـ OTP واستلام إشعار التوصيل (الانحراف: {pod.DistanceVarianceMeters}م) وتم إرساله للإشراف."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
