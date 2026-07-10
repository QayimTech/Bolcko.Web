using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.User;

namespace Bolcko.Web.App.Areas.Delivery.Controllers
{
    [Area("Delivery")]
    [Authorize(Roles = "DeliveryDriver, DeliveryCompanyUser")]
    public class HomeController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;

        public HomeController(IServiceManager serviceManager, UserManager<User> userManager)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null)
            {
                TempData["Info"] = "لم يتم ربط حسابك بملف مندوب. يرجى التسجيل كمندوب أولاً.";
                return View("Register");
            }

            if (!driver.IsApproved)
            {
                TempData["Warning"] = "حسابك قيد المراجعة من الإدارة. سيتم إشعارك عند الموافقة.";
                return View("PendingApproval");
            }

            var myJobs = await _serviceManager.DeliveryService.GetDriverJobsAsync(driver.Id);
            var availableJobs = await _serviceManager.DeliveryService.GetAvailableJobsAsync();
            var myBids = await _serviceManager.DeliveryService.GetDriverBidsAsync(driver.Id);

            ViewBag.Driver = driver;
            ViewBag.AvailableJobs = availableJobs;
            ViewBag.MyBids = myBids;
            return View(myJobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceBid(int jobId, decimal bidAmount)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null) return BadRequest();

            try
            {
                await _serviceManager.DeliveryService.PlaceBidAsync(jobId, driver.Id, bidAmount);
                TempData["Success"] = "تم تقديم عرضك بنجاح! سيتم إشعارك عند قبوله.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int jobId, Bolcko.Domain.Enums.DeliveryJobStatus status)
        {
            try
            {
                await _serviceManager.DeliveryService.UpdateJobStatusAsync(jobId, status);
                TempData["Success"] = "تم تحديث الحالة بنجاح!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var driver = await _serviceManager.DeliveryService.GetDriverByUserIdAsync(user.Id);
            if (driver == null) return BadRequest();

            await _serviceManager.DeliveryService.UpdateDriverAvailabilityAsync(driver.Id, !driver.IsAvailable);
            TempData["Success"] = driver.IsAvailable ? "تم تغيير حالتك إلى غير متاح." : "تم تغيير حالتك إلى متاح.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string vehicleType, string? vehiclePlateNumber, string? licenseNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            try
            {
                await _serviceManager.DeliveryService.RegisterDriverAsync(user.Id, null, vehicleType, vehiclePlateNumber, licenseNumber);
                TempData["Success"] = "تم تسجيل طلبك بنجاح! سيتم مراجعته من الإدارة.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطأ: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
