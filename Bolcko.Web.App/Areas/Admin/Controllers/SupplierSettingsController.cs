using System;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Supplier;
using Bolcko.Domain.Entities.Supplier;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class SupplierSettingsController : Controller
    {
        private readonly ISupplierApiService _supplierApiService;

        public SupplierSettingsController(ISupplierApiService supplierApiService)
        {
            _supplierApiService = supplierApiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var configs = await _supplierApiService.GetAllConfigsAsync();
            var qannas = await _supplierApiService.GetActiveConfigAsync("qannas") ?? new SupplierProviderConfig
            {
                SupplierName = "شركة القناص لتجارة الجملة",
                SupplierKey = "qannas",
                BaseUrl = "https://api.qannasjo.com/",
                ApiPhoneNumber = "0782023800",
                ApiPassword = "Qannas@100",
                ApiEmail = "qannasco@gmail.com",
                PickupAddressLine = "عمان - رأس العين - مستودع القناص",
                PickupCityId = 395,
                PickupRegionId = 33,
                PickupVillageId = 18076,
                ContactPersonName = "موظف بلوكو - مستودع رأس العين",
                ContactPhoneNumber = "0782023800",
                AutoDispatchEnabled = true,
                IsActive = true
            };

            ViewBag.AllConfigs = configs;
            return View(qannas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(SupplierProviderConfig config)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(config.SupplierName) || string.IsNullOrWhiteSpace(config.SupplierKey))
                {
                    TempData["ErrorMessage"] = "يرجى تعبئة اسم المورد ومفتاح الربط بشكل صحيح.";
                    return RedirectToAction(nameof(Index));
                }

                var success = await _supplierApiService.SaveConfigAsync(config);
                if (success)
                {
                    TempData["SuccessMessage"] = $"تم حفظ إعدادات المورد '{config.SupplierName}' ونقطة استلام رأس العين بنجاح!";
                }
                else
                {
                    TempData["ErrorMessage"] = "فشل حفظ إعدادات المورد في قاعدة البيانات.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"حدث خطأ أثناء الحفظ: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TestConnection(int id)
        {
            try
            {
                var configs = await _supplierApiService.GetAllConfigsAsync();
                var config = configs.Find(c => c.Id == id) ?? await _supplierApiService.GetActiveConfigAsync("qannas");
                
                if (config == null)
                {
                    return Json(new { success = false, message = "لم يتم العثور على إعدادات المورد." });
                }

                var isConnected = await _supplierApiService.TestConnectionAsync(config);
                if (isConnected)
                {
                    return Json(new { 
                        success = true, 
                        message = $"تم الاتصال بنجاح وتوليد الـ Token لحساب '{config.ContactPersonName}' ({config.ApiPhoneNumber})!",
                        customerId = config.ExternalCustomerId,
                        userId = config.ExternalUserId
                    });
                }

                return Json(new { success = false, message = "فشل تسجيل الدخول وجلب التوكن. يرجى التأكد من رقم الهاتف وكلمة المرور." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطأ: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DispatchOrderToSupplier(int orderId, [FromServices] Bolcko.Domain.Interfaces.IUnitOfWork unitOfWork)
        {
            try
            {
                var order = await unitOfWork.Orders.GetOrderByIdWithItemsAsync(orderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "الطلب غير موجود." });
                }

                if (order.HasOversizedItems)
                {
                    return Json(new { success = false, message = "هذا الطلب يحتوي مواد ضخمة، لا يتم تحويله لطلبات الجملة العادية." });
                }

                var result = await _supplierApiService.CreatePurchaseOrderAsync(order);
                return Json(new { 
                    success = result.Success, 
                    message = result.Message, 
                    orderNumber = result.ExternalOrderNumber,
                    detail = result.ErrorDetail 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"حدث خطأ: {ex.Message}" });
            }
        }
    }
}
