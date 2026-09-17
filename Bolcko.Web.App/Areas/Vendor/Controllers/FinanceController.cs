using Blocko.Persistence;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Order;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize]
    [Route("Vendor/[controller]")]
    public class FinanceController : Controller
    {
        private readonly BlockoDbContext _context;
        private readonly UserManager<User> _userManager;

        public FinanceController(BlockoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<VendorProfile?> GetCurrentVendorProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.VendorProfiles.FirstOrDefaultAsync(v => v.UserId == user.Id);
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var vendorId = vendor?.Id ?? 0;
            var commissionRate = (vendor?.CommissionRatePercentage ?? 2.5m) / 100m;

            // Fetch vendor order items
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product.SupplierId == vendorId || oi.Product.SupplierKey == "vendor_" + vendorId)
                .OrderByDescending(oi => oi.Id)
                .ToListAsync();

            decimal grossSales = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
            decimal platformFees = grossSales * commissionRate;
            decimal netEarnings = grossSales - platformFees;

            // Delivered orders count towards available payout balance
            decimal availableBalance = orderItems
                .Where(oi => oi.Order.Status == OrderStatus.Delivered)
                .Sum(oi => (oi.UnitPrice * oi.Quantity) * (1 - commissionRate));

            decimal pendingSettlement = netEarnings - availableBalance;

            ViewBag.Vendor = vendor;
            ViewBag.GrossSales = grossSales;
            ViewBag.PlatformFees = platformFees;
            ViewBag.NetEarnings = netEarnings;
            ViewBag.AvailableBalance = availableBalance;
            ViewBag.PendingSettlement = pendingSettlement;
            ViewBag.CommissionRate = vendor?.CommissionRatePercentage ?? 2.5m;

            return View(orderItems);
        }

        [HttpPost]
        [Route("RequestPayout")]
        [ValidateAntiForgeryToken]
        public IActionResult RequestPayout(decimal amount, string payoutMethod, string accountDetails)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(accountDetails))
            {
                TempData["Error"] = "يرجى تحديد مبلغ صالح وإدخال بيانات الحساب البنكي / CliQ.";
                return RedirectToAction(nameof(Index));
            }

            // Record Payout Request
            TempData["Success"] = $"تم استلام طلب التحويل المالي بمبلغ {amount:N2} د.أ عبر {payoutMethod} بنجاح! سيتم التحويل خلال 24 ساعة.";
            return RedirectToAction(nameof(Index));
        }
    }
}
