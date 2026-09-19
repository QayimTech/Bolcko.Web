using Blocko.Persistence;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Financing.DTOs;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Route("Shop/[controller]")]
    public class FinancingController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<int>>? _roleManager;
        private readonly IWebHostEnvironment? _env;

        public FinancingController(
            IServiceManager serviceManager,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole<int>>? roleManager = null,
            IWebHostEnvironment? env = null)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _env = env;
        }

        /// <summary>
        /// صفحة تسجيل وتأهيل المستثمر المالي ومكافحة غسل الأموال (Investor Onboarding & AML KYC)
        /// </summary>
        [HttpGet("InvestorJoin")]
        [AllowAnonymous]
        public IActionResult InvestorJoin()
        {
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Investor"))
            {
                return RedirectToAction(nameof(InvestorDashboard));
            }

            var model = new Bolcko.Web.App.Areas.Shop.Models.InvestorJoinViewModel();
            return View(model);
        }

        /// <summary>
        /// معالجة تسجيل المستثمر والتحقق من الملاءمة المالية ومكافحة غسل الأموال
        /// </summary>
        [HttpPost("InvestorJoin")]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InvestorJoin(Bolcko.Web.App.Areas.Shop.Models.InvestorJoinViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "البريد الإلكتروني مسجل مسبقاً كمستثمر أو عميل.");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FirstName = model.LegalName,
                LastName = $"({model.InvestorCategory})",
                CompanyName = model.LegalName,
                BusinessRegistrationNumber = model.NationalIdOrCr,
                UserType = UserType.Investor,
                RegistrationDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                {
                    ModelState.AddModelError("", err.Description);
                }
                return View(model);
            }

            if (_roleManager != null && !await _roleManager.RoleExistsAsync("Investor"))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>("Investor"));
            }

            await _userManager.AddToRoleAsync(user, "Investor");

            // Handle KYC documents upload
            if (_env != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "investors", user.Id.ToString());
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                if (model.IdentityDocument != null && model.IdentityDocument.Length > 0)
                {
                    string idPath = Path.Combine(uploadsFolder, "ID_" + Path.GetFileName(model.IdentityDocument.FileName));
                    using var stream = new FileStream(idPath, FileMode.Create);
                    await model.IdentityDocument.CopyToAsync(stream);
                }

                if (model.IbanDocument != null && model.IbanDocument.Length > 0)
                {
                    string ibanPath = Path.Combine(uploadsFolder, "IBAN_" + Path.GetFileName(model.IbanDocument.FileName));
                    using var stream = new FileStream(ibanPath, FileMode.Create);
                    await model.IbanDocument.CopyToAsync(stream);
                }
            }

            await _signInManager.SignInAsync(user, isPersistent: true);

            TempData["SuccessMessage"] = "مرحباً بك في منصة بلكو المالية! تم إنشاء محفظتك الاستثمارية وتوثيق بيانات الملاءمة ومكافحة غسل الأموال بنجاح.";
            return RedirectToAction(nameof(InvestorDashboard), new { registered = true });
        }

        /// <summary>
        /// شاشة معالج طرح عطاء تمويل المرابحة للمقاول (Contractor Murabaha Tender Wizard)
        /// </summary>
        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create(string? source, decimal? estimatedAmount)
        {
            var materialTypes = await _serviceManager.FinancingService.GetActiveMaterialTypesAsync();

            ViewBag.MaterialTypes = materialTypes;
            ViewBag.EstimatedAmount = estimatedAmount ?? 0m;
            ViewBag.Source = source ?? "";

            return View();
        }

        /// <summary>
        /// صفحة الهبوط الترويجية لبوابة المستثمرين وحاسبة العوائد التفاعلية (/Invest)
        /// </summary>
        [HttpGet("/Invest")]
        [HttpGet("Invest")]
        [AllowAnonymous]
        public async Task<IActionResult> Invest()
        {
            var tenders = await _serviceManager.FinancingService.GetOpenTendersAsync();
            return View("~/Areas/Shop/Views/Financing/Invest.cshtml", tenders);
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

        /// <summary>
        /// موافقة وتمويل المستثمر للعطاء بنظام الوكالة الشرعية (مع دعم تسجيل حساب فوري بنقرة واحدة)
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

            // 1. If already logged in, get user id
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) funderId = id;
            }
            // 2. If not logged in, but provided email and password -> auto-register / sign in
            else if (!string.IsNullOrWhiteSpace(request.FunderEmail) && !string.IsNullOrWhiteSpace(request.Password))
            {
                var existingUser = await _userManager.FindByEmailAsync(request.FunderEmail.Trim());
                if (existingUser != null)
                {
                    var isPasswordValid = await _userManager.CheckPasswordAsync(existingUser, request.Password);
                    if (isPasswordValid)
                    {
                        await _signInManager.SignInAsync(existingUser, isPersistent: true);
                        funderId = existingUser.Id;
                    }
                    else
                    {
                        return Json(new { success = false, message = "هذا البريد الإلكتروني مسجل مسبقاً، لكن كلمة المرور المدخلة غير صحيحة. يرجى إدخال كلمة المرور الصحيحة لحسابك للمتابعة." });
                    }
                }
                else
                {
                    var nameParts = (request.FunderName ?? "").Trim().Split(' ');
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "مستثمر";
                    var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "ممول";

                    var newUser = new User
                    {
                        UserName = request.FunderEmail.Trim(),
                        Email = request.FunderEmail.Trim(),
                        PhoneNumber = request.FunderPhone?.Trim(),
                        FirstName = firstName,
                        LastName = lastName,
                        UserType = UserType.Investor,
                        EmailConfirmed = true,
                        RegistrationDate = DateTime.UtcNow
                    };

                    var createRes = await _userManager.CreateAsync(newUser, request.Password);
                    if (createRes.Succeeded)
                    {
                        try
                        {
                            await _userManager.AddToRoleAsync(newUser, "Investor");
                        }
                        catch { }

                        await _signInManager.SignInAsync(newUser, isPersistent: true);
                        funderId = newUser.Id;
                    }
                    else
                    {
                        var firstErr = createRes.Errors.FirstOrDefault()?.Description ?? "فشل إنشاء حساب المستثمر.";
                        return Json(new { success = false, message = firstErr });
                    }
                }
            }

            var result = await _serviceManager.FinancingService.FundTenderAsync(request, funderId);
            if (!result)
            {
                return Json(new { success = false, message = "تعذر تمويل العطاء، قد يكون العطاء ممولاً مسبقاً أو غير متاح." });
            }

            return Json(new
            {
                success = true,
                redirectUrl = "/Shop/Financing/InvestorDashboard",
                message = "تم تمويل العطاء وشراء البضاعة بنجاح! تم إصدار أمر الشراء (PO) وتوليد عقد الوكالة الشرعي في محفظتك."
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
        /// رفع إثبات التسليم الموقعي وفحص الـ Geofencing (مؤمن ومحكوم بأدوار السائقين والإشراف)
        /// </summary>
        [HttpPost]
        [Route("SubmitPOD")]
        [Authorize(Roles = "DeliveryDriver, DeliveryCompanyUser, Admin, SuperAdmin")]
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
        /// سداد وتسوية عطاء المرابحة (Settlement Engine) - مؤمن بصلاحيات SuperAdmin / Admin أو المقاول صاحب العطاء (Zero-Trust)
        /// </summary>
        [HttpPost]
        [Route("Settle/{id}")]
        [Authorize(Roles = "SuperAdmin,Admin,Contractor,FinanceManager,DashboardUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settle(int id)
        {
            var tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            if (tender == null)
            {
                return NotFound(new { success = false, message = "العطاء المطلوب غير موجود." });
            }

            var isPrivileged = User.IsInRole("SuperAdmin") || User.IsInRole("Admin") || User.IsInRole("FinanceManager") || User.IsInRole("DashboardUser");
            if (!isPrivileged)
            {
                var currentUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUserPhone = User.FindFirstValue(ClaimTypes.MobilePhone) ?? User.Identity?.Name;

                bool isOwner = false;
                if (int.TryParse(currentUserIdStr, out var currentUserId) && tender.ContractorId == currentUserId)
                {
                    isOwner = true;
                }
                else if (!string.IsNullOrEmpty(currentUserPhone) && !string.IsNullOrEmpty(tender.ContractorPhone) && currentUserPhone.Contains(tender.ContractorPhone))
                {
                    isOwner = true;
                }

                if (!isOwner)
                {
                    return StatusCode(403, new { success = false, message = "غير مصرح لك بتسوية هذا العطاء (انتهاك ملكية العطاء IDOR)." });
                }
            }

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

        /// <summary>
        /// استعراض وتنزيل عقد الوكالة الشرعي بالمرابحة للأمر بالشراء (Wakala Agreement)
        /// </summary>
        [HttpGet]
        [Route("Contract/{code}")]
        [Route("WakalaContract/{code}")]
        public async Task<IActionResult> Contract(string code)
        {
            FinancingTenderDto? tender = null;
            if (int.TryParse(code, out var id))
            {
                tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            }
            if (tender == null)
            {
                tender = await _serviceManager.FinancingService.GetTenderByTrackingCodeAsync(code);
            }

            return View("Contract", tender);
        }

        /// <summary>
        /// استعراض وطباعة شهادة القبض والتملك الحكمي الشرعي (Constructive Possession Certificate)
        /// </summary>
        [HttpGet]
        [Route("PossessionNotice/{code}")]
        [Route("ConstructivePossession/{code}")]
        public async Task<IActionResult> PossessionNotice(string code)
        {
            FinancingTenderDto? tender = null;
            if (int.TryParse(code, out var id))
            {
                tender = await _serviceManager.FinancingService.GetTenderByIdAsync(id);
            }
            if (tender == null)
            {
                tender = await _serviceManager.FinancingService.GetTenderByTrackingCodeAsync(code);
            }

            if (tender == null)
            {
                return NotFound("شهادة القبض الحكمي غير موجودة أو لم يتم إصدارها بعد.");
            }

            return View("PossessionNotice", tender);
        }

        /// <summary>
        /// طلب سحب الأرباح ورأس المال المسترد عبر CliQ أو التحويل البنكي (IBAN)
        /// </summary>
        [HttpPost]
        [Route("RequestPayout")]
        public async Task<IActionResult> RequestPayout([FromBody] InvestorPayoutRequestDto request)
        {
            if (request == null || request.AmountJod <= 0)
            {
                return Json(new { success = false, message = "يرجى تحديد مبلغ السحب بشكل صحيح." });
            }

            int? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var id)) userId = id;
            }

            try
            {
                var success = await _serviceManager.FinancingService.RequestPayoutAsync(request, userId);
                return Json(new
                {
                    success = true,
                    message = $"تم تقديم طلب سحب مبلغ {request.AmountJod:N2} د.أ بنجاح عبر {(request.PayoutMethod == "CliQ" ? "نظام كليك الفوري (CliQ)" : "التحويل البنكي المباشر")}! سيتم إيداع المبلغ خلال لحظات."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}