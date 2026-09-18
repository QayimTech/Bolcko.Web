using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Financing.DTOs;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Entities.User.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area(Shop)]
    [Route(Investor/Marketplace)]
    [Route(Investor/[controller]/[action])]
    public class InvestorPortalController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public InvestorPortalController(
            IServiceManager serviceManager,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        [Route(")]
 [Route(Index)]
 [AllowAnonymous]
 public async Task<IActionResult> Index()
 {
 var tenders = await _serviceManager.FinancingService.GetOpenTendersAsync();
 return View(~/Areas/Shop/Views/Financing/Index.cshtml, tenders);
 }

 [HttpGet]
 [Route(Details/{id})]
 [AllowAnonymous]
 public async Task<IActionResult> Details(int id)
 {
 var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
 if (tender == null) return NotFound();
 return View(~/Areas/Shop/Views/Financing/Details.cshtml, tender);
 }

 [HttpGet]
 [Route(Dashboard)]
 [Route(Portfolio)]
 [Authorize(Roles = Investor, Admin, SuperAdmin, DashboardUser)]
 public async Task<IActionResult> Portfolio()
 {
 int? userId = null;
 string? phone = null;

 if (User.Identity?.IsAuthenticated == true)
 {
 var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
 if (int.TryParse(idStr, out var id)) userId = id;
 }

 var dashboard = await _serviceManager.FinancingService.GetInvestorDashboardAsync(userId, phone);
 return View(~/Areas/Shop/Views/Financing/InvestorDashboard.cshtml, dashboard);
 }

 [HttpGet]
 [Route(Contract/{code})]
 [AllowAnonymous]
 public async Task<IActionResult> Contract(string code)
 {
 FinancingTenderDto? tender = null;
 if (int.TryParse(code, out var id))
 {
 tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
 }
 else
 {
 tender = await _serviceManager.FinancingService.GetTenderByTrackingCodeAsync(code);
 }

 if (tender == null) return NotFound(عقد الوكالة غير موجود أو تم إلغاؤه.);
 return View(~/Areas/Shop/Views/Financing/Contract.cshtml, tender);
 }
 }
}
