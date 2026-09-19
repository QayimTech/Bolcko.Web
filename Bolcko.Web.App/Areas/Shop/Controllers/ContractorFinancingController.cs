using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Financing.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Route("Contractor/Financing")]
    [Route("Contractor/[controller]/[action]")]
    [Authorize(Roles = "Contractor, Admin, SuperAdmin, DashboardUser")]
    public class ContractorFinancingController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public ContractorFinancingController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpGet]
        [Route("")]
        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            int? userId = null;
            string? phone = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) userId = id;
            }

            var dashboard = await _serviceManager.FinancingService.GetContractorDashboardAsync(userId, phone);
            return View("~/Areas/Shop/Views/Financing/ContractorDashboard.cshtml", dashboard);
        }

        [HttpGet]
        [Route("Create")]
        [AllowAnonymous]
        public async Task<IActionResult> Create(string? source, decimal? estimatedAmount)
        {
            var materialTypes = await _serviceManager.FinancingService.GetActiveMaterialTypesAsync();
            ViewBag.MaterialTypes = materialTypes;
            ViewBag.EstimatedAmount = estimatedAmount ?? 0m;
            ViewBag.Source = source ?? "";

            return View("~/Areas/Shop/Views/Financing/Create.cshtml");
        }

        [HttpPost]
        [Route("CreateFromBOQ")]
        [AllowAnonymous]
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

            try
            {
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
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
