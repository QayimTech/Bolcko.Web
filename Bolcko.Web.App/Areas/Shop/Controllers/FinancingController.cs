using Blocko.Persistence;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Financing.DTOs;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
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
        private readonly BlockoDbContext _context;

        public FinancingController(
            IServiceManager serviceManager,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            BlockoDbContext context)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        /// <summary>
        /// شاشة معالج طرح عطاء تمويل المرابحة للمقاول (Contractor Murabaha Tender Wizard)
        /// </summary>
        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create(string? source, decimal? estimatedAmount)
        {
            var materialTypes = await _context.MaterialTypes
                .Where(m => m.IsActive)
                .OrderBy(m => m.SortOrder)
                .ToListAsync();

            ViewBag.MaterialTypes = materialTypes;
            ViewBag.EstimatedAmount = estimatedAmount ?? 0m;
            ViewBag.Source = source ?? "";

            return View();
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
                    var signInRes = await _signInManager.PasswordSignInAsync(existingUser, request.Password, isPersistent: true, lockoutOnFailure: false);
                    if (signInRes.Succeeded)
                    {
                        funderId = existingUser.Id;
                    }
                    else
                    {
                        return Json(new { success = false, message = "البريد مسجل مسبقاً وكلمة المرور غير مطابقة. يرجى تسجيل الدخول أولاً." });
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
                        PhoneNumber = request.FunderPhone.Trim(),
                        FirstName = firstName,
                        LastName = lastName,
                        UserType = UserType.Investor,
                        EmailConfirmed = true,
                        RegistrationDate = DateTime.UtcNow
                    };

                    var createRes = await _userManager.CreateAsync(newUser, request.Password);
                    if (createRes.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(newUser, "Investor");
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
        /// رفع إثبات التسليم الموقعي وفحص الـ Geofencing
        /// </summary>
        [HttpPost]
        [Route("SubmitPOD")]
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
        /// سداد وتسوية عطاء المرابحة (Settlement Engine)
        /// </summary>
        [HttpPost]
        [Route("Settle/{id}")]
        public async Task<IActionResult> Settle(int id)
        {
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
    }
}