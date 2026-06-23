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

            var orders = await _serviceManager.OrderService.GetUserOrdersAsync(user.Id);
            var tenders = await _serviceManager.TenderService.GetTendersByUserAsync(user.Id); 
            var projects = await _serviceManager.ProjectService.GetUserProjectsAsync(user.Id);

            ViewBag.User = user;
            ViewBag.Orders = orders;
            ViewBag.Tenders = tenders;
            ViewBag.Projects = projects;

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
                ViewBag.Message = "إذا كان الحساب مسجلاً لدينا، فقد تم إرسال رابط إعادة تعيين كلمة المرور. يرجى مراجعة بريدك الإلكتروني.";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", new { area = "Shop", userId = user.Id, token = token }, protocol: Request.Scheme);

            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; direction: rtl; text-align: right; padding: 20px; border: 1px solid #e2e8f0; max-width: 600px; margin: auto;'>
                    <h2 style='color: #151b26;'>إعادة تعيين كلمة المرور - BLOCKO</h2>
                    <p>لقد تلقينا طلباً لإعادة تعيين كلمة المرور الخاصة بحسابك في BLOCKO.</p>
                    <p>يرجى النقر على الزر أدناه لإعادة تعيين كلمة المرور الخاصة بك:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{callbackUrl}' style='background-color: #E8A020; color: #151b26; padding: 12px 24px; text-decoration: none; font-weight: bold; border-radius: 6px;'>إعادة تعيين كلمة المرور</a>
                    </div>
                    <p>أو انسخ الرابط التالي والصقه في متصفحك:</p>
                    <p style='word-break: break-all; color: #64748b;'>{callbackUrl}</p>
                    <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #94a3b8;'>إذا لم تطلب هذا التغيير، يرجى تجاهل هذا البريد الإلكتروني.</p>
                </div>";

            await _emailSender.SendEmailAsync(email, "إعادة تعيين كلمة المرور - BLOCKO", emailBody);

            ViewBag.Message = "إذا كان الحساب مسجلاً لدينا، فقد تم إرسال رابط إعادة تعيين كلمة المرور. يرجى مراجعة بريدك الإلكتروني.";
            ViewBag.DebugResetLink = callbackUrl;

            return View();
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
    }
}
