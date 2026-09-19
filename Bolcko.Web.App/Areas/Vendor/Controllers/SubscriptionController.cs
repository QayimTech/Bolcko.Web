using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Persistence;
using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.Subscription;
using Bolcko.Domain.Entities.Subscription.DTOs;
using Bolcko.Domain.Entities.User;
using Bolcko.Web.App.Areas.Vendor.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Route("Vendor/[controller]")]
    public class SubscriptionController : Controller
    {
        private readonly BlockoDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly UserManager<User> _userManager;

        public SubscriptionController(
            BlockoDbContext context,
            ISubscriptionService subscriptionService,
            UserManager<User> userManager)
        {
            _context = context;
            _subscriptionService = subscriptionService;
            _userManager = userManager;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            var vendor = await _context.VendorProfiles
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.UserId == userId || v.Id == 1);

            var plans = (await _subscriptionService.GetPlansAsync("Vendor")).ToList();
            var activeSub = await _subscriptionService.GetUserActiveSubscriptionAsync(userId);
            var history = (await _subscriptionService.GetUserSubscriptionHistoryAsync(userId)).ToList();

            var currentTier = vendor?.SubscriptionTier ?? "Gold";
            var commission = currentTier == "Gold" ? 1.5m : (currentTier == "Silver" ? 2.5m : 3.5m);
            var boost = currentTier == "Gold" ? 1.5 : (currentTier == "Silver" ? 1.2 : 1.0);

            var model = new VendorSubscriptionViewModel
            {
                VendorId = vendor?.Id ?? 1,
                CompanyName = vendor?.CompanyNameAr ?? "مجموعة القنّاص لمواد البناء",
                ActiveTier = currentTier,
                ActiveTierNameAr = currentTier == "Gold" ? "الشريك الذهبي (Gold Partner)" : (currentTier == "Silver" ? "الشريك الفضي (Silver Partner)" : "التاجر الأساسي (Standard)"),
                CurrentCommissionRate = commission,
                CatalogBoostMultiplier = boost,
                SubscriptionStartDate = activeSub?.StartDate ?? DateTime.UtcNow.AddMonths(-1),
                SubscriptionEndDate = activeSub?.EndDate ?? DateTime.UtcNow.AddMonths(11),
                DaysRemaining = activeSub != null ? Math.Max(0, (int)(activeSub.EndDate - DateTime.UtcNow).TotalDays) : 335,
                AutoRenew = true,
                IsActive = true,
                AvailablePlans = plans,
                BillingHistory = history
            };

            return View(model);
        }

        [HttpPost("Upgrade")]
        public async Task<IActionResult> Upgrade([FromBody] UpgradePlanRequest request)
        {
            if (request == null || request.PlanId <= 0)
            {
                return Json(new { success = false, message = "بيانات الباقة غير صحيحة." });
            }

            try
            {
                int userId = GetCurrentUserId();
                var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == request.PlanId);
                if (plan == null)
                {
                    return Json(new { success = false, message = "الباقة المختارة غير موجودة." });
                }

                var subReq = new SubscribeRequestDto
                {
                    PlanId = request.PlanId,
                    BillingCycle = request.BillingCycle,
                    PaymentTransactionRef = !string.IsNullOrEmpty(request.CliqAliasOrRef) ? request.CliqAliasOrRef : $"CLIQ-SUB-{Guid.NewGuid().ToString("N")[..8].ToUpper()}"
                };

                var success = await _subscriptionService.SubscribeAsync(userId, subReq);
                if (!success)
                {
                    return Json(new { success = false, message = "تعذر إتمام عملية الاشتراك. يرجى مراجعة بيانات الدفع." });
                }

                // Determine Tier Name & Commission
                string tier = plan.NameEn.Contains("Gold", StringComparison.OrdinalIgnoreCase) ? "Gold" :
                              (plan.NameEn.Contains("Silver", StringComparison.OrdinalIgnoreCase) ? "Silver" : "Standard");
                decimal commission = tier == "Gold" ? 1.5m : (tier == "Silver" ? 2.5m : 3.5m);
                double boostMultiplier = tier == "Gold" ? 1.5 : (tier == "Silver" ? 1.2 : 1.0);

                var vendor = await _context.VendorProfiles.FirstOrDefaultAsync(v => v.UserId == userId || v.Id == 1);
                if (vendor != null)
                {
                    vendor.SubscriptionTier = tier;
                    vendor.CommissionRatePercentage = commission;
                    vendor.IsGoldVerified = tier == "Gold";
                    _context.VendorProfiles.Update(vendor);
                }

                // Boost catalog items search ranking score
                var products = await _context.Products.Where(p => p.SupplierId == (vendor != null ? vendor.Id : 1) || p.SupplierKey == "qannas").ToListAsync();
                foreach (var prod in products)
                {
                    prod.SearchRankingScore = boostMultiplier;
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    tier = tier,
                    commissionRate = commission,
                    boost = boostMultiplier,
                    message = $"تهانينا! تم ترقية باقتك إلى [{plan.NameAr}] بنجاح، وتفعيل عمولة مخفضة {commission}% وتطبيق مضاعف الظهور {boostMultiplier}x على كافة منتجاتك في المتجر."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int id))
            {
                return id;
            }
            return 13; // Default Al-Qannas Vendor User
        }
    }
}
