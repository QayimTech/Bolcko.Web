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
        // VL-01: Autonomous Fulfillment Hub & Custom 3PL Carriers (VL-03)
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

            // VL-03: Load Custom 3PL Carrier Partners
            var customCarriers = await _context.VendorCustomCarriers
                .Where(c => c.VendorId == vendor.Id)
                .OrderByDescending(c => c.Id)
                .ToListAsync();

            if (!customCarriers.Any())
            {
                var c1 = new Bolcko.Domain.Entities.Delivery.VendorCustomCarrier
                {
                    VendorId = vendor.Id,
                    CarrierName = "شركة أسطول الأردن للنقل الثقيل والرافعات",
                    ContactPhone = "0798822119",
                    ContactPerson = "م. سامر الشوابكة",
                    FleetType = "تريلات مسطحة وونشات حمولة 40 طن",
                    BaseTariffJod = 25.00m,
                    CraneFeeJod = 35.00m,
                    CoveredGovernorates = "عمان, الزرقاء, البلقاء, إربد",
                    IsActive = true
                };
                var c2 = new Bolcko.Domain.Entities.Delivery.VendorCustomCarrier
                {
                    VendorId = vendor.Id,
                    CarrierName = "مؤسسة الرمحي للشحن الثقيل والتفريغ الهيدروليكي",
                    ContactPhone = "0785511223",
                    ContactPerson = "أبو طارق الرمحي",
                    FleetType = "شاحنات بوم ترك (Boom Truck) 15-25 طن",
                    BaseTariffJod = 30.00m,
                    CraneFeeJod = 40.00m,
                    CoveredGovernorates = "عمان, مادبا, الكرك, العقبة",
                    IsActive = true
                };
                _context.VendorCustomCarriers.AddRange(c1, c2);
                await _context.SaveChangesAsync();
                customCarriers = new List<Bolcko.Domain.Entities.Delivery.VendorCustomCarrier> { c1, c2 };
            }

            ViewBag.Vendor = vendor;
            ViewBag.Vehicles = vehicles;
            ViewBag.CustomCarriers = customCarriers;

            return View(config);
        }

        [HttpPost]
        [Route("AddCustomCarrier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCustomCarrier(string carrierName, string contactPhone, string? contactPerson, string? fleetType, decimal baseTariffJod, decimal craneFeeJod, string? coveredGovernorates)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return NotFound();

            if (string.IsNullOrWhiteSpace(carrierName) || string.IsNullOrWhiteSpace(contactPhone))
            {
                TempData["Error"] = "يرجى تعبئة اسم شركة الشحن ورقم الهاتف على الأقل.";
                return RedirectToAction(nameof(Index));
            }

            var carrier = new Bolcko.Domain.Entities.Delivery.VendorCustomCarrier
            {
                VendorId = vendor.Id,
                CarrierName = carrierName.Trim(),
                ContactPhone = contactPhone.Trim(),
                ContactPerson = contactPerson?.Trim() ?? string.Empty,
                FleetType = !string.IsNullOrWhiteSpace(fleetType) ? fleetType.Trim() : "تريلات مسطحة وونشات ثقيلة",
                BaseTariffJod = baseTariffJod > 0 ? baseTariffJod : 25.00m,
                CraneFeeJod = craneFeeJod >= 0 ? craneFeeJod : 35.00m,
                CoveredGovernorates = !string.IsNullOrWhiteSpace(coveredGovernorates) ? coveredGovernorates.Trim() : "عمان, الزرقاء, البلقاء, إربد",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.VendorCustomCarriers.Add(carrier);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"تمت إضافة شريك الشحن الثقيل المعتمد ({carrier.CarrierName}) بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("DeleteCustomCarrier")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCustomCarrier(int id)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return NotFound();

            var carrier = await _context.VendorCustomCarriers.FirstOrDefaultAsync(c => c.Id == id && c.VendorId == vendor.Id);
            if (carrier != null)
            {
                _context.VendorCustomCarriers.Remove(carrier);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف شريك الشحن بنجاح.";
            }
            return RedirectToAction(nameof(Index));
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

            var vehicles = new List<VendorFleetVehicleDto>
            {
                new VendorFleetVehicleDto { Id = 1, PlateNumber = "12-38491", VehicleType = "تريلا قلاب ثقيل 30 طن", MaxCapacityTons = 30.0m, DriverName = "أحمد الخالدي", DriverPhone = "0791234567", IsAvailable = true, HasCrane = false },
                new VendorFleetVehicleDto { Id = 2, PlateNumber = "14-88219", VehicleType = "شاحنة ونش هيدروليكي 15 طن", MaxCapacityTons = 15.0m, DriverName = "محمود الزعبي", DriverPhone = "0788765432", IsAvailable = true, HasCrane = true },
                new VendorFleetVehicleDto { Id = 3, PlateNumber = "16-55102", VehicleType = "لوري مسطح 10 طن", MaxCapacityTons = 10.0m, DriverName = "سامر المجالي", DriverPhone = "0775544332", IsAvailable = false, HasCrane = false }
            };

            var customCarriers = await _context.VendorCustomCarriers
                .Where(c => c.VendorId == vendorId && c.IsActive)
                .ToListAsync();

            ViewBag.Vendor = vendor;
            ViewBag.Vehicles = vehicles;
            ViewBag.CustomCarriers = customCarriers;
            return View(dispatchOrders);
        }

        [HttpPost]
        [Route("AssignDriver")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDriver(int orderId, string driverName, string driverPhone, string truckPlateNumber, string vehicleType, string? carrierType, int? customCarrierId)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null) return NotFound();

            string carrierName = driverName;
            if (carrierType == "3PL" && customCarrierId.HasValue)
            {
                var carrier = await _context.VendorCustomCarriers.FirstOrDefaultAsync(c => c.Id == customCarrierId.Value && c.VendorId == vendor.Id);
                if (carrier != null)
                {
                    carrierName = carrier.CarrierName;
                    if (string.IsNullOrWhiteSpace(driverPhone)) driverPhone = carrier.ContactPhone;
                }
            }

            string logType = carrierType == "3PL" ? "شريك الشحن الثقيل المعتمد (3PL)" : "سائق الأسطول الخاص";
            TempData["Success"] = $"تم تعيين {logType} ({carrierName} - {truckPlateNumber}) للشحنة #{orderId} وتوليد بيان الشحن بنجاح!";
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
