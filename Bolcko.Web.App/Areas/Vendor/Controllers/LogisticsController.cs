using Blocko.Persistence;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Delivery.DTOs;
using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize]
    [Route("Vendor/[controller]")]
    public class LogisticsController : Controller
    {
        private readonly BlockoDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _env;

        public LogisticsController(BlockoDbContext context, UserManager<User> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        private async Task<VendorProfile?> GetCurrentVendorProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.VendorProfiles.FirstOrDefaultAsync(v => v.UserId == user.Id);
        }

        // ==========================================
        // VL-01: Autonomous Fulfillment Hub
        // ==========================================
        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null)
            {
                TempData["Error"] = "يرجى توثيق ملف المورد الخاص بك للوصول لمركز اللوجستيات.";
                return RedirectToAction("Index", "Dashboard");
            }

            var config = new VendorFulfillmentConfigDto
            {
                VendorId = vendor.Id,
                CompanyName = vendor.CompanyNameAr,
                Mode = VendorFulfillmentMode.PlatformPool,
                StandardDeliveryFeeJod = 25.00m,
                CraneOffloadingFeeJod = 35.00m,
                FreeDeliveryThresholdJod = 1500.00m,
                EstimatedSlaHours = 24,
                TotalActiveTrucks = 4,
                MaxTruckPayloadTons = 30.0m,
                HasCraneUnloadingEquipped = true,
                CoveredGovernorates = new List<string> { "عمان", "الزرقاء", "البلقاء", "إربد", "المفرق", "مأدبا" }
            };

            var vehicles = new List<VendorFleetVehicleDto>
            {
                new VendorFleetVehicleDto { Id = 1, PlateNumber = "12-38491", VehicleType = "تريلا قلاب ثقيل 30 طن", MaxCapacityTons = 30.0m, DriverName = "أحمد الخالدي", DriverPhone = "0791234567", IsAvailable = true, HasCrane = false },
                new VendorFleetVehicleDto { Id = 2, PlateNumber = "14-88219", VehicleType = "شاحنة ونش هيدروليكي 15 طن", MaxCapacityTons = 15.0m, DriverName = "محمود الزعبي", DriverPhone = "0788765432", IsAvailable = true, HasCrane = true },
                new VendorFleetVehicleDto { Id = 3, PlateNumber = "16-55102", VehicleType = "لوري مسطح 10 طن", MaxCapacityTons = 10.0m, DriverName = "سامر المجالي", DriverPhone = "0775544332", IsAvailable = false, HasCrane = false }
            };

            ViewBag.Vendor = vendor;
            ViewBag.Vehicles = vehicles;

            return View(config);
        }

        [HttpPost]
        [Route("SaveFulfillmentConfig")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveFulfillmentConfig(VendorFulfillmentConfigDto model, string? governoratesList)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(governoratesList))
            {
                vendor.CoverageAreasJson = System.Text.Json.JsonSerializer.Serialize(governoratesList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث استراتيجية التوريد والخدمات اللوجستية بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        // ==========================================
        // VL-02: Driver Assignment & Weighbridge Desk
        // ==========================================
        [HttpGet]
        [Route("Dispatch")]
        public async Task<IActionResult> Dispatch()
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return RedirectToAction("Index", "Dashboard");

            var vendorId = vendor.Id;

            // Fetch active orders for this vendor
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.ShippingAddress)
                .Include(oi => oi.Order)
                    .ThenInclude(o => o.User)
                .Include(oi => oi.Product)
                .Where(oi => oi.Product != null && (oi.Product.SupplierId == vendorId || oi.Product.SupplierKey == "vendor_" + vendorId))
                .OrderByDescending(oi => oi.OrderId)
                .Take(20)
                .ToListAsync();

            var dispatchOrders = new List<VendorDispatchOrderDto>();

            if (orderItems.Any())
            {
                foreach (var oi in orderItems)
                {
                    dispatchOrders.Add(new VendorDispatchOrderDto
                    {
                        OrderId = oi.OrderId,
                        TrackingCode = $"BLK-{oi.OrderId:D5}",
                        ProjectTitle = $"مشروع توريد #{oi.OrderId}",
                        CustomerName = oi.Order?.User != null ? ($"{oi.Order.User.FirstName} {oi.Order.User.LastName}".Trim() + (string.IsNullOrEmpty(oi.Order.User.CompanyName) ? "" : $" ({oi.Order.User.CompanyName})")) : "عميل بلوكو المعتمد",
                        CustomerPhone = oi.Order?.User?.PhoneNumber ?? "0790000000",
                        DestinationAddress = oi.Order?.ShippingAddress?.AddressLine1 ?? "موقع الورشة الإنشائية",
                        DestinationCity = oi.Order?.ShippingAddress?.City ?? "عمان",
                        MaterialName = oi.Product?.Name ?? "مواد بناء",
                        Quantity = oi.Quantity,
                        Unit = oi.Product?.UnitOfMeasure ?? "طن",
                        TotalAmountJod = oi.UnitPrice * oi.Quantity,
                        Status = oi.Order?.Status == Bolcko.Domain.Enums.OrderStatus.Delivered ? "Delivered" : "ReadyForLoading",
                        OrderDate = oi.Order?.OrderDate ?? DateTime.UtcNow
                    });
                }
            }
            else
            {
                // Demonstration active dispatch items for vendor
                dispatchOrders.Add(new VendorDispatchOrderDto
                {
                    OrderId = 10421,
                    TrackingCode = "BLK-10421",
                    ProjectTitle = "مشروع برج دابوق السكني",
                    CustomerName = "م. عمر القضاة (شركة الإعمار الحديثة)",
                    CustomerPhone = "0795544123",
                    DestinationAddress = "دابوق - قرب قصر العبد",
                    DestinationCity = "عمان",
                    Latitude = 31.9921,
                    Longitude = 35.8451,
                    MaterialName = "حديد تسليح 14 ملم - Grade 60 أردني",
                    Quantity = 24.5m,
                    Unit = "طن",
                    TotalAmountJod = 14210.00m,
                    AssignedDriverName = "أحمد الخالدي",
                    AssignedDriverPhone = "0791234567",
                    TruckPlateNumber = "12-38491",
                    VehicleType = "تريلا قلاب ثقيل 30 طن",
                    WeighbridgeTicketNo = "WB-2026-9921",
                    TareWeightTons = 14.20m,
                    GrossWeightTons = 38.70m,
                    NetWeightTons = 24.50m,
                    WeighedAt = DateTime.UtcNow.AddHours(-2),
                    Status = "WeighedAndDispatched",
                    OrderDate = DateTime.UtcNow.AddHours(-4)
                });

                dispatchOrders.Add(new VendorDispatchOrderDto
                {
                    OrderId = 10422,
                    TrackingCode = "BLK-10422",
                    ProjectTitle = "مجمع الفحيص الطبي",
                    CustomerName = "شركة البنيان الهندسية",
                    CustomerPhone = "0788899112",
                    DestinationAddress = "الفحيص - شارع البكالوريا",
                    DestinationCity = "البلقاء",
                    Latitude = 32.0125,
                    Longitude = 35.7820,
                    MaterialName = "حجر رويشد نخب أول مفجر 25 سم",
                    Quantity = 450.0m,
                    Unit = "م²",
                    TotalAmountJod = 5850.00m,
                    Status = "ReadyForLoading",
                    OrderDate = DateTime.UtcNow.AddHours(-1)
                });
            }

            ViewBag.Vendor = vendor;
            return View(dispatchOrders);
        }

        [HttpPost]
        [Route("AssignDriver")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int orderId, string driverName, string driverPhone, string truckPlateNumber, string vehicleType)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return NotFound();

            TempData["Success"] = $"تم تعيين السائق ({driverName} - {truckPlateNumber}) للشحنة #{orderId} بنجاح!";
            return RedirectToAction(nameof(Dispatch));
        }

        [HttpPost]
        [Route("UploadWeighbridgeTicket")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadWeighbridgeTicket(int orderId, string ticketNo, decimal tareWeight, decimal grossWeight, IFormFile? ticketFile)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return NotFound();

            decimal netWeight = grossWeight - tareWeight;
            if (netWeight <= 0)
            {
                TempData["Error"] = "خطأ في أوزان القبان: الوزن الإجمالي يجب أن يكون أكبر من وزن الشاحنة فارغة.";
                return RedirectToAction(nameof(Dispatch));
            }

            string? fileUrl = null;
            if (ticketFile != null && ticketFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "weighbridge");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"wb_{orderId}_{Guid.NewGuid():N}{Path.GetExtension(ticketFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ticketFile.CopyToAsync(stream);
                }
                fileUrl = $"/uploads/weighbridge/{fileName}";
            }

            TempData["Success"] = $"تم تسجيل تذكرة القبان #{ticketNo} بصافي حمولة {netWeight:N2} طن وتحديث بيان الشحنة!";
            return RedirectToAction(nameof(Dispatch));
        }

        // ==========================================
        // Digital Dispatch Manifest (بيان الحمولة A4)
        // ==========================================
        [HttpGet]
        [Route("Manifest/{id}")]
        public async Task<IActionResult> Manifest(int id)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            
            var manifest = new VendorDispatchOrderDto
            {
                OrderId = id,
                TrackingCode = $"BLK-{id:D5}",
                ProjectTitle = "مشروع برج دابوق الإنشائي",
                CustomerName = "م. عمر القضاة (شركة الإعمار الحديثة)",
                CustomerPhone = "0795544123",
                DestinationAddress = "دابوق - الحوض 4 - قطعة 112",
                DestinationCity = "عمان",
                Latitude = 31.9921,
                Longitude = 35.8451,
                MaterialName = "حديد تسليح 14 ملم أردني Grade 60 (مواصفات قياسية معتمدة)",
                Quantity = 24.5m,
                Unit = "طن",
                TotalAmountJod = 14210.00m,
                AssignedDriverName = "أحمد الخالدي",
                AssignedDriverPhone = "0791234567",
                TruckPlateNumber = "12-38491",
                VehicleType = "تريلا قلاب 30 طن",
                WeighbridgeTicketNo = "WB-2026-9921",
                TareWeightTons = 14.20m,
                GrossWeightTons = 38.70m,
                NetWeightTons = 24.50m,
                WeighedAt = DateTime.UtcNow.AddHours(-1),
                Status = "WeighedAndDispatched",
                OrderDate = DateTime.UtcNow
            };

            ViewBag.Vendor = vendor;
            return View(manifest);
        }
    }
}
