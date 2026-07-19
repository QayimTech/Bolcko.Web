using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.User;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class AccountController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(IServiceManager serviceManager, UserManager<User> userManager, SignInManager<User> signInManager, IEmailSender emailSender)
        {
            _serviceManager = serviceManager;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                // Block Admin/DashboardUser from logging in via Shop — they must use the Admin login page
                if (await _userManager.IsInRoleAsync(user, "Admin") ||
                    await _userManager.IsInRoleAsync(user, "DashboardUser"))
                {
                    ViewBag.Error = "هذا الحساب مخصص للإدارة فقط. يرجى تسجيل الدخول من لوحة التحكم.";
                    return View();
                }

                var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: rememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    if (await _userManager.IsInRoleAsync(user, "DeliveryCompanyUser") ||
                        await _userManager.IsInRoleAsync(user, "DeliveryDriver"))
                    {
                        return RedirectToAction("Index", "Home", new { area = "Delivery" });
                    }
                    return RedirectToAction("Index");
                }
            }

            ViewBag.Error = "بيانات الدخول غير صحيحة";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "كلمات المرور غير متطابقة");
                ViewBag.Error = "كلمات المرور غير متطابقة";
                return View(user);
            }

            user.UserName = user.Email;
            user.UserType = UserType.Customer;
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                // Default role for new users
                await _userManager.AddToRoleAsync(user, "Customer");
                await _signInManager.SignInAsync(user, isPersistent: true);
                
                return RedirectToAction("Index");
            }

            foreach (var error in result.Errors)
            {
                var key = "";
                if (error.Code.Contains("Password"))
                    key = "Password";
                else if (error.Code.Contains("Email"))
                    key = "Email";
                else if (error.Code.Contains("UserName"))
                    key = "Username";
                
                ModelState.AddModelError(key, error.Description);
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            return View(user);
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (await _userManager.IsInRoleAsync(user, "DeliveryCompanyUser") ||
                await _userManager.IsInRoleAsync(user, "DeliveryDriver"))
            {
                return RedirectToAction("Index", "Home", new { area = "Delivery" });
            }

            var orders = await _serviceManager.OrderService.GetUserOrdersAsync(user.Id);
            var tenders = await _serviceManager.TenderService.GetTendersByUserAsync(user.Id); 
            var projects = await _serviceManager.ProjectService.GetUserProjectsAsync(user.Id);

            ViewBag.User = user;
            ViewBag.Orders = orders.ToList();
            ViewBag.Tenders = tenders.ToList();
            ViewBag.Projects = projects.ToList();

            return View();
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        public async Task<IActionResult> Orders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var orders = (await _serviceManager.OrderService.GetUserOrdersAsync(user.Id)).ToList();
            ViewBag.User = user;
            
            return View(orders);
        }

        public async Task<IActionResult> Quotes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var tenders = (await _serviceManager.TenderService.GetTendersByUserAsync(user.Id)).ToList();
            ViewBag.User = user;
            
            return View(tenders);
        }

        public async Task<IActionResult> Projects()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var projects = await _serviceManager.ProjectService.GetUserProjectsAsync(user.Id);
            ViewBag.User = user;
            
            return View(projects);
        }

        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var order = await _serviceManager.OrderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != user.Id)
            {
                return NotFound();
            }

            ViewBag.User = user;
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmDelivery(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var order = await _serviceManager.OrderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != user.Id)
                return NotFound();

            if (order.Status == Bolcko.Domain.Enums.OrderStatus.Shipped)
            {
                await _serviceManager.OrderService.UpdateOrderStatusAsync(id, Bolcko.Domain.Enums.OrderStatus.Delivered);
                TempData["SuccessMessage"] = "تم تأكيد استلام الطلبية بنجاح. شكراً لثقتك بـ BLOCKO!";
            }

            return RedirectToAction("OrderDetails", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatus(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var order = await _serviceManager.OrderService.GetOrderByIdAsync(id);
            if (order == null || order.UserId != user.Id) return NotFound();

            var statusLabel = order.Status switch
            {
                Bolcko.Domain.Enums.OrderStatus.Pending    => "قيد الانتظار",
                Bolcko.Domain.Enums.OrderStatus.Processing => "قيد المعالجة",
                Bolcko.Domain.Enums.OrderStatus.Shipped    => "تم الشحن",
                Bolcko.Domain.Enums.OrderStatus.Delivered  => "تم التسليم",
                Bolcko.Domain.Enums.OrderStatus.Cancelled  => "ملغي",
                _ => order.Status.ToString()
            };

            return Json(new { status = order.Status.ToString(), statusLabel, orderId = id });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "الرجاء إدخال البريد الإلكتروني";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Message = "إذا كان الحساب مسجلاً لدينا، فقد تم إرسال رمز التحقق. يرجى مراجعة بريدك الإلكتروني.";
                return View();
            }

            // Generate a 6-digit numeric OTP code
            var random = new Random();
            var otpCode = random.Next(100000, 999999).ToString();

            // Set dynamic claim or temporary property (using UserClaims for persistence or TempData/Session)
            // Let's use ASP.NET Identity's UserClaims to store the OTP securely
            var existingClaims = await _userManager.GetClaimsAsync(user);
            var otpClaim = existingClaims.FirstOrDefault(c => c.Type == "PasswordResetOTP");
            if (otpClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, otpClaim);
            }
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("PasswordResetOTP", otpCode));

            var otpTimeClaim = existingClaims.FirstOrDefault(c => c.Type == "PasswordResetOTPExpiry");
            if (otpTimeClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, otpTimeClaim);
            }
            var expiryTime = DateTime.UtcNow.AddMinutes(15).ToString("O");
            await _userManager.AddClaimAsync(user, new System.Security.Claims.Claim("PasswordResetOTPExpiry", expiryTime));

            // Generate dynamic reset token that we will store in TempData or query string later once verified
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Send Beautiful HTML email using EmailTemplates
            string emailBody = Blocko.Services.Helpers.EmailTemplates.GetOtpTemplate(otpCode, "15");

            await _emailSender.SendEmailAsync(email, "رمز التحقق لإعادة تعيين كلمة المرور - BLOCKO", emailBody);

            TempData["UserEmail"] = email;
            TempData["ResetToken"] = resetToken;

            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = TempData["UserEmail"] as string;
            var token = TempData["ResetToken"] as string;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }

            // Keep TempData alive for the next postback
            TempData.Keep("UserEmail");
            TempData.Keep("ResetToken");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            var email = TempData["UserEmail"] as string;
            var token = TempData["ResetToken"] as string;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(otp))
            {
                ViewBag.Error = "حدث خطأ ما أو انتهت صلاحية الجلسة.";
                return View();
            }

            TempData.Keep("UserEmail");
            TempData.Keep("ResetToken");

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ViewBag.Error = "المستخدم غير موجود.";
                return View();
            }

            var claims = await _userManager.GetClaimsAsync(user);
            var otpClaim = claims.FirstOrDefault(c => c.Type == "PasswordResetOTP");
            var otpTimeClaim = claims.FirstOrDefault(c => c.Type == "PasswordResetOTPExpiry");

            if (otpClaim == null || otpClaim.Value != otp.Trim())
            {
                ViewBag.Error = "رمز التحقق المدخل غير صحيح.";
                return View();
            }

            if (otpTimeClaim != null && DateTime.TryParse(otpTimeClaim.Value, out DateTime expiry) && expiry < DateTime.UtcNow)
            {
                ViewBag.Error = "لقد انتهت صلاحية رمز التحقق. يرجى طلب رمز جديد.";
                return View();
            }

            // Clean up claims since OTP is successfully verified
            await _userManager.RemoveClaimAsync(user, otpClaim);
            if (otpTimeClaim != null)
            {
                await _userManager.RemoveClaimAsync(user, otpTimeClaim);
            }

            // Successfully verified OTP. Redirect to reset password page with email and token
            return RedirectToAction("ResetPassword", new { userId = user.Id, token = token });
        }

        [HttpGet]
        public IActionResult ResetPassword(int userId, string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("رمز تعيين كلمة المرور غير صالح.");
            }
            ViewBag.UserId = userId;
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int userId, string token, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "كلمات المرور غير متطابقة";
                ViewBag.UserId = userId;
                ViewBag.Token = token;
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound("المستخدم غير موجود");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "تمت إعادة تعيين كلمة المرور بنجاح. يمكنك الآن تسجيل الدخول باستخدام كلمة المرور الجديدة.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = string.Join(", ", result.Errors.Select(e => e.Description));
            ViewBag.UserId = userId;
            ViewBag.Token = token;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Track(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var order = await _serviceManager.OrderService.GetOrderByIdAsync(orderId);
            if (order == null || order.UserId != user.Id) return NotFound();

            var job = await _serviceManager.DeliveryService.GetJobByOrderIdAsync(orderId);

            ViewBag.Order = order;
            return View(job);
        }
    }
}
