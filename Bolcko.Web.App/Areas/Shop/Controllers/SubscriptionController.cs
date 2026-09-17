using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Subscription.DTOs;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    [Route("Shop/[controller]")]
    public class SubscriptionController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public SubscriptionController(
            IServiceManager serviceManager,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        [Route("")]
        [Route("Plans")]
        public async Task<IActionResult> Plans(string? role = null)
        {
            var plans = await _serviceManager.SubscriptionService.GetPlansAsync(role);
            UserSubscriptionDto? currentSub = null;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idStr, out var uid))
                {
                    currentSub = await _serviceManager.SubscriptionService.GetUserActiveSubscriptionAsync(uid);
                }
            }

            ViewBag.ActiveRole = role ?? (User.IsInRole("Investor") ? "Investor" : (User.IsInRole("Contractor") ? "Contractor" : (User.IsInRole("Vendor") ? "Vendor" : "Investor")));
            ViewBag.CurrentSub = currentSub;
            return View(plans);
        }

        [HttpPost]
        [Route("Subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request)
        {
            if (request == null || request.PlanId <= 0)
            {
                return Json(new { success = false, message = "بيانات الاشتراك غير صحيحة." });
            }

            int userId = 0;

            if (User.Identity?.IsAuthenticated == true)
            {
                var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(idStr, out userId))
                {
                    return Json(new { success = false, message = "يرجى تسجيل الدخول أولاً." });
                }
            }
            else
            {
                // Frictionless Onboarding for new subscriber
                if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return Json(new { success = false, message = "يرجى تسجيل الدخول أو إدخال البريد الإلكتروني وكلمة المرور لإنشاء حسابك." });
                }

                var cleanEmail = request.Email.Trim();
                var existingUser = await _userManager.FindByEmailAsync(cleanEmail);
                if (existingUser != null)
                {
                    var signRes = await _signInManager.PasswordSignInAsync(existingUser, request.Password, isPersistent: true, lockoutOnFailure: false);
                    if (!signRes.Succeeded)
                    {
                        return Json(new { success = false, message = "البريد مسجل مسبقاً وكلمة المرور غير صحيحة. يرجى تسجيل الدخول أولاً." });
                    }
                    userId = existingUser.Id;
                }
                else
                {
                    var plan = await _serviceManager.SubscriptionService.GetPlanByIdAsync(request.PlanId);
                    var targetRole = plan?.TargetRole ?? "Customer";

                    var nameParts = (request.FullName ?? "مشترك جديد").Trim().Split(' ');
                    var firstName = nameParts.Length > 0 ? nameParts[0] : "مشترك";
                    var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "جديد";

                    var userType = targetRole == "Investor" ? UserType.Investor : (targetRole == "Contractor" ? UserType.Contractor : UserType.Customer);

                    var newUser = new User
                    {
                        UserName = cleanEmail,
                        Email = cleanEmail,
                        PhoneNumber = request.PhoneNumber?.Trim() ?? "",
                        FirstName = firstName,
                        LastName = lastName,
                        UserType = userType,
                        EmailConfirmed = true,
                        RegistrationDate = DateTime.UtcNow
                    };

                    var createRes = await _userManager.CreateAsync(newUser, request.Password);
                    if (!createRes.Succeeded)
                    {
                        var err = createRes.Errors.FirstOrDefault()?.Description ?? "فشل إنشاء الحساب.";
                        return Json(new { success = false, message = err });
                    }

                    await _userManager.AddToRoleAsync(newUser, targetRole);
                    await _signInManager.SignInAsync(newUser, isPersistent: true);
                    userId = newUser.Id;
                }
            }

            var success = await _serviceManager.SubscriptionService.SubscribeAsync(userId, request);
            if (!success)
            {
                return Json(new { success = false, message = "تعذر إتمام عملية الاشتراك، يرجى المحاولة لاحقاً." });
            }

            return Json(new
            {
                success = true,
                message = "تم تفعيل باقة الاشتراك بنجاح! تم ترقية ميزات حسابك وسقفك التمويلي فوراً.",
                redirectUrl = "/Shop/Subscription/MySubscription"
            });
        }

        [HttpGet]
        [Route("MySubscription")]
        public async Task<IActionResult> MySubscription()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login", "Account", new { area = "Shop", returnUrl = "/Shop/Subscription/MySubscription" });
            }

            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var uid))
            {
                return RedirectToAction("Index", "Home");
            }

            var activeSub = await _serviceManager.SubscriptionService.GetUserActiveSubscriptionAsync(uid);
            var history = await _serviceManager.SubscriptionService.GetUserSubscriptionHistoryAsync(uid);

            ViewBag.ActiveSub = activeSub;
            ViewBag.History = history;

            return View();
        }

        [HttpPost]
        [Route("Cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            if (User.Identity?.IsAuthenticated != true) return Json(new { success = false });

            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(idStr, out var uid)) return Json(new { success = false });

            var res = await _serviceManager.SubscriptionService.CancelSubscriptionAsync(uid, id);
            return Json(new { success = res, message = res ? "تم إلغاء التجديد التلقائي للاشتراك." : "تعذر إلغاء الاشتراك." });
        }
    }
}
