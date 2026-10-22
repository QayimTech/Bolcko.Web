using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<User> _userManager;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _webHostEnvironment;

        public DashboardController(
            IUnitOfWork unitOfWork,
            UserManager<User> userManager,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Vendor/Dashboard or /Vendor/Dashboard/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Vendor" });
            }

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();

            if (profile != null)
            {
                ViewBag.ActiveTenders = await GetMatchingTendersForVendorAsync(profile);
            }

            // If user has no vendor profile yet, redirect them to join or provide fallback
            return View(profile);
        }

        // POST: /Vendor/Dashboard/UploadKycDocs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadKycDocs(
            IFormFile? commercialRegistrationDoc,
            IFormFile? vocationalLicenseDoc,
            IFormFile? taxCertificateDoc,
            IFormFile? qualityCertificatesDoc)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Vendor" });
            }

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();

            if (profile == null)
            {
                TempData["ErrorMessage"] = "الملف الشخصي للمورد غير موجود.";
                return RedirectToAction(nameof(Index));
            }

            bool updated = false;

            if (commercialRegistrationDoc != null && commercialRegistrationDoc.Length > 0)
            {
                var url = await SaveKycDocAsync(commercialRegistrationDoc, "CR", user.Id);
                if (url != null)
                {
                    profile.CommercialRegistrationDocUrl = url;
                    updated = true;
                }
            }

            if (vocationalLicenseDoc != null && vocationalLicenseDoc.Length > 0)
            {
                var url = await SaveKycDocAsync(vocationalLicenseDoc, "VOC", user.Id);
                if (url != null)
                {
                    profile.VocationalLicenseDocUrl = url;
                    updated = true;
                }
            }

            if (taxCertificateDoc != null && taxCertificateDoc.Length > 0)
            {
                var url = await SaveKycDocAsync(taxCertificateDoc, "TAX", user.Id);
                if (url != null)
                {
                    profile.TaxCertificateDocUrl = url;
                    updated = true;
                }
            }

            if (qualityCertificatesDoc != null && qualityCertificatesDoc.Length > 0)
            {
                var url = await SaveKycDocAsync(qualityCertificatesDoc, "QC", user.Id);
                if (url != null)
                {
                    profile.QualityCertificatesDocUrl = url;
                    updated = true;
                }
            }

            if (updated)
            {
                _unitOfWork.VendorProfiles.Update(profile);
                await _unitOfWork.CompleteAsync();
                TempData["SuccessMessage"] = "تم تحديث ورفع وثائق الاعتماد القانونية بنجاح، جاري مراجعتها من قبل فريق التدقيق.";
            }
            else
            {
                TempData["ErrorMessage"] = "لم يتم اختيار أي ملفات صالحة للرفع (.pdf, .png, .jpg).";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// مكتب استقبال وتسعير طلبات الأسعار للكميات الكبيرة (RFQ Response Desk)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RfqDesk()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Vendor" });

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();

            if (profile != null)
            {
                ViewBag.ActiveTenders = await GetMatchingTendersForVendorAsync(profile);
            }

            return View(profile);
        }

        /// <summary>
        /// تقديم عرض سعر رسمي ملزم من المورد (Submit Vendor Binding Supply Quote)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVendorBid(int tenderId, decimal totalPrice, decimal deliveryFee, int deliveryDays, int validityDays, string? notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();
            if (profile == null) return BadRequest("الملف الشخصي للمورد غير موجود.");

            var tender = await _unitOfWork.Tenders.GetByIdAsync(tenderId);
            if (tender == null) return NotFound("طلب عرض السعر غير موجود.");

            tender.TotalQuotedAmount = totalPrice;
            tender.DeliveryFee = deliveryFee;
            tender.EstimatedDeliveryDays = deliveryDays;
            tender.BidValidityDays = validityDays;
            tender.VendorOfferNotes = notes;
            tender.AwardedSupplierId = profile.Id;
            tender.Status = Bolcko.Domain.Enums.TenderStatus.Priced;

            _unitOfWork.Tenders.Update(tender);
            await _unitOfWork.CompleteAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "تم تقديم عرض السعر بنجاح إلى العميل." });
            }

            TempData["SuccessMessage"] = "تم تقديم عرض السعر بنجاح إلى العميل.";
            return RedirectToAction(nameof(RfqDesk));
        }

        private async Task<List<Bolcko.Domain.Entities.Tender.Tender>> GetMatchingTendersForVendorAsync(VendorProfile profile)
        {
            var openTenders = await _unitOfWork.Tenders.GetOpenTendersAsync();
            var list = openTenders.Where(t =>
                // 1. Direct assignment to this vendor
                t.TargetVendorId == profile.Id
                // 2. Subscribed private tender for Gold/Silver
                || (t.TenderScope == "SubscribedPrivate" && (profile.SubscriptionTier == "Gold" || profile.SubscriptionTier == "Silver"))
                // 3. Open nearest tender matching city / unassigned
                || (t.TenderScope == "OpenNearest" || string.IsNullOrEmpty(t.TenderScope) || t.TargetVendorId == null)
            ).ToList();

            return list;
        }

        /// <summary>
        /// مركز الربط البرمجي وتكامل الـ ERP (ERP Integration Hub)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Integration()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Vendor" });

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();

            return View(profile);
        }

        /// <summary>
        /// كشف حساب المستحقات والضمان المالي (Escrow Payouts & Settlements)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Escrow()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "Vendor" });

            var profileList = await _unitOfWork.VendorProfiles.FindAsync(v => v.UserId == user.Id);
            var profile = profileList.FirstOrDefault();

            return View(profile);
        }

        private async Task<string?> SaveKycDocAsync(IFormFile? file, string docType, int userId)
        {
            if (file == null || file.Length == 0) return null;
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(ext)) return null;

            var fileName = $"{docType}_{userId}_{Guid.NewGuid():N}{ext}";
            var storage = HttpContext.RequestServices.GetRequiredService<Bolcko.Web.App.Services.IDocumentStorage>();
            return await storage.SaveAsync(file, "vendor-kyc", fileName);
        }
    }
}
