using System;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SecurityCenterController : Controller
    {
        private readonly ISecurityAuditService _securityAuditService;
        private readonly IAnalyticsService _analyticsService;

        public SecurityCenterController(ISecurityAuditService securityAuditService, IAnalyticsService analyticsService)
        {
            _securityAuditService = securityAuditService;
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
        {
            ViewBag.PendingThreats = await _securityAuditService.GetPendingThreatLogsAsync(100);
            ViewBag.Blacklist = await _securityAuditService.GetActiveBlacklistAsync();
            ViewBag.TrafficKpis = await _analyticsService.GetTrafficKpisAsync(startDate, endDate);

            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BlacklistIp(string ipAddress, string reason, int? durationHours)
        {
            var userId = User.Identity?.Name ?? "Admin";
            var success = await _securityAuditService.BlacklistIpAsync(ipAddress, string.IsNullOrWhiteSpace(reason) ? "حظر بناء على محاولات مشبوهة" : reason, userId, durationHours);

            return Json(new { success, message = success ? $"تم حظر الـ IP {ipAddress} بنجاح!" : "تعذّر إجراء الحظر." });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveBlacklist(string ipAddress)
        {
            var success = await _securityAuditService.RemoveFromBlacklistAsync(ipAddress);
            return Json(new { success, message = success ? $"تم فك حظر الـ IP {ipAddress} بنجاح!" : "تعذّر إزالة الحظر." });
        }

        [HttpPost]
        public async Task<IActionResult> DismissThreat(long auditLogId)
        {
            var success = await _securityAuditService.DismissThreatAsync(auditLogId);
            return Json(new { success });
        }

        [HttpGet]
        public async Task<IActionResult> GetSecurityAlertsJson()
        {
            var pendingThreats = await _securityAuditService.GetPendingThreatLogsAsync(10);
            var kpis = await _analyticsService.GetTrafficKpisAsync();

            return Json(new
            {
                pendingThreatsCount = kpis.SuspiciousThreats,
                threats = pendingThreats
            });
        }
    }
}
