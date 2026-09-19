using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Web.App.Areas.Shop.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Shop.Controllers
{
    [Area("Shop")]
    public class StoreController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public StoreController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        /// <summary>
        /// صفحة المعرض الرقمي المعتمد للمورد / المصنع (/Store/{slug} & /Shop/Store/{slug})
        /// Branded Vendor Showroom Page & Multi-Seller Buy Box (VM-02, QT1-127)
        /// </summary>
        [HttpGet("/Store/{slug}")]
        [HttpGet("/Shop/Store/{slug}")]
        public async Task<IActionResult> Index(string slug, string? category = null)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return RedirectToAction("Index", "Home", new { area = "Shop" });
            }

            var slugKey = slug.Trim().ToLowerInvariant();

            // Known Vendor Registry
            var vendorProfiles = new Dictionary<string, VendorShowroomViewModel>
            {
                ["al-qannas"] = new VendorShowroomViewModel
                {
                    Slug = "al-qannas",
                    Name = "مجموعة القنّاص لتوريد مواد البناء",
                    NameEn = "Al-Qannas Building Materials Group",
                    Tagline = "الموزع والوكيل المعتمد لكبرى مصانع الإسمنت والحديد والخرسانة في الأردن",
                    Description = "نقدم حلول توريد متكاملة للمشاريع الإنشائية والتجارية والسكنية بأسعار الجملة المباشرة من المصنع، مع أسطول شاحنات مجهز بأوناش تفريغ هيدروليكية وفحص قبان معتمد.",
                    City = "عمّان - سحاب الصناعية",
                    WarehouseAddress = "مدينة الملك عبدالله الثاني ابن الحسين الصناعية - سحاب، الشارع الرئيسي",
                    Latitude = 31.8756,
                    Longitude = 35.9861,
                    Rating = 4.95,
                    ReviewCount = 342,
                    CompletedOrders = 1420,
                    OnTimeDeliveryRate = 99.4,
                    IsGoldSupplier = true,
                    IsFactoryDirect = true,
                    FulfillmentMode = "أسطول نقل خاص + ونش تفريغ (Own Fleet)",
                    CommercialRegistrationNumber = "CR-19948201",
                    NationalTaxNumber = "TAX-99482010"
                },
                ["al-manaseer"] = new VendorShowroomViewModel
                {
                    Slug = "al-manaseer",
                    Name = "شركة المناصير للإسمنت والخرسانة الجاهزة",
                    NameEn = "Manaseer Cement & Concrete",
                    Tagline = "رواد صناعة الإسمنت والخرسانة الجاهزة والمواد التخصصية بأعلى المواصفات العالمية",
                    Description = "أكبر شبكة محطات خلط خرسانة ومصانع إسمنت تغطي كافة محافظات المملكة بأعلى معايير الجودة والمطابقة للمواصفات الأردنية.",
                    City = "عمّان - القسطل",
                    WarehouseAddress = "مجمع المناصير الصناعي - القسطل، طريق المطار",
                    Latitude = 31.7580,
                    Longitude = 35.9230,
                    Rating = 4.98,
                    ReviewCount = 512,
                    CompletedOrders = 2890,
                    OnTimeDeliveryRate = 99.7,
                    IsGoldSupplier = true,
                    IsFactoryDirect = true,
                    FulfillmentMode = "أسطول خلاطات ومضخات مركزي (Platform Fleet)",
                    CommercialRegistrationNumber = "CR-10293844",
                    NationalTaxNumber = "TAX-10293844"
                },
                ["arab-steel"] = new VendorShowroomViewModel
                {
                    Slug = "arab-steel",
                    Name = "مصانع الأردنية لحديد وصلب البناء",
                    NameEn = "Jordan Arab Steel Rolling Mills",
                    Tagline = "حديد تسليح معتمد عالي المقاومة (Grade 60 / Grade 500) مباشرة من خطوط الدرفلة",
                    Description = "توريد قضبان حديد التسليح وشبك الأرضيات والصلب الإنشائي بشهادات فحص مخبري رسمية وأوزان قبان دقيقة.",
                    City = "الزرقاء - المنطقة الحرة",
                    WarehouseAddress = "المنطقة الحرة الزرقاء - بوابة الشحن الثقيل",
                    Latitude = 32.0911,
                    Longitude = 36.1264,
                    Rating = 4.88,
                    ReviewCount = 189,
                    CompletedOrders = 980,
                    OnTimeDeliveryRate = 98.9,
                    IsGoldSupplier = true,
                    IsFactoryDirect = true,
                    FulfillmentMode = "شاحنات تريلا ثقيلة (Heavy Freight Pool)",
                    CommercialRegistrationNumber = "CR-33441199",
                    NationalTaxNumber = "TAX-33441199"
                }
            };

            var profile = vendorProfiles.TryGetValue(slugKey, out var p) ? p : vendorProfiles["al-qannas"];

            // Fetch products for showroom
            var allProducts = (await _serviceManager.ProductService.GetAllProductsAsync()).ToList();

            // Set vendor attribution on products
            foreach (var prod in allProducts)
            {
                prod.SupplierName = profile.Name;
                prod.SupplierSlug = profile.Slug;
                prod.SupplierIsVerified = profile.IsGoldSupplier;
                prod.SupplierCity = profile.City;
                prod.SupplierRating = profile.Rating;
            }

            var categories = allProducts
                .Where(x => !string.IsNullOrEmpty(x.CategoryName))
                .Select(x => x.CategoryName!)
                .Distinct()
                .ToList();

            profile.AvailableCategories = categories;
            profile.SelectedCategory = category;

            if (!string.IsNullOrEmpty(category))
            {
                profile.Products = allProducts.Where(x => x.CategoryName?.Equals(category, StringComparison.OrdinalIgnoreCase) == true).Take(24).ToList();
            }
            else
            {
                profile.Products = allProducts.Take(24).ToList();
            }

            return View("~/Areas/Shop/Views/Store/Index.cshtml", profile);
        }
    }
}
