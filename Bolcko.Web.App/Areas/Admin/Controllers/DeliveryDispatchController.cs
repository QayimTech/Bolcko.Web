using Blocko.Services.Interfaces;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class DeliveryDispatchController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public DeliveryDispatchController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        // ============================
        // DELIVERY JOBS (Dispatch)
        // ============================

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var pagedJobs = await _serviceManager.DeliveryService.GetPagedAllJobsAsync(page, pageSize);
            return View(pagedJobs);
        }

        public async Task<IActionResult> JobDetails(int id)
        {
            var job = await _serviceManager.DeliveryService.GetJobByIdAsync(id);
            if (job == null) return NotFound();
            return View(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(int orderId, decimal deliveryFee)
        {
            try
            {
                await _serviceManager.DeliveryService.CreateJobForOrderAsync(orderId, deliveryFee);
                TempData["Success"] = "تم طرح طلب التوصيل في السوق بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int jobId, int driverId, decimal fee)
        {
            try
            {
                await _serviceManager.DeliveryService.AssignJobToDriverAsync(jobId, driverId, fee);
                TempData["Success"] = "تم تعيين المندوب للطلب بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptBid(int bidId, int jobId)
        {
            try
            {
                await _serviceManager.DeliveryService.AcceptBidAsync(bidId);
                TempData["Success"] = "تم قبول العرض وتعيين المندوب بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateJobStatus(int jobId, DeliveryJobStatus status)
        {
            try
            {
                await _serviceManager.DeliveryService.UpdateJobStatusAsync(jobId, status);
                TempData["Success"] = "تم تحديث حالة التوصيل بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        // ============================
        // DRIVERS MANAGEMENT
        // ============================

        public async Task<IActionResult> Drivers(int page = 1, int pageSize = 10)
        {
            var pagedDrivers = await _serviceManager.DeliveryService.GetPagedDriversAsync(page, pageSize);
            return View(pagedDrivers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDriver(int driverId)
        {
            try
            {
                await _serviceManager.DeliveryService.ApproveDriverAsync(driverId);
                TempData["Success"] = "تم تفعيل حساب المندوب بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("Drivers");
        }

        // ============================
        // DELIVERY COMPANIES
        // ============================

        public async Task<IActionResult> Companies(int page = 1, int pageSize = 10)
        {
            var pagedCompanies = await _serviceManager.DeliveryService.GetPagedCompaniesAsync(page, pageSize);
            return View(pagedCompanies);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCompany(string name, string? email, string? phoneNumber, string? commercialRegister, decimal baseRate)
        {
            try
            {
                await _serviceManager.DeliveryService.CreateCompanyAsync(name, email, phoneNumber, commercialRegister, baseRate);
                TempData["Success"] = $"تم إضافة شركة التوصيل '{name}' بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ: {ex.Message}";
            }
            return RedirectToAction("Companies");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendCompanyEmail(int jobId)
        {
            try
            {
                await _serviceManager.DeliveryService.SendDeliveryDocumentsToCompanyAsync(jobId);
                TempData["Success"] = "تم إرسال مستندات التوصيل (PDF & Excel) إلى شركة التوصيل بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء الإرسال: {ex.Message}";
            }
            return RedirectToAction("JobDetails", new { id = jobId });
        }
    }
}
