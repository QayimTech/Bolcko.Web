using Blocko.Persistence;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Product;
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

namespace Bolcko.Web.App.Areas.Vendor.Controllers
{
    [Area("Vendor")]
    [Authorize]
    [Route("Vendor/[controller]")]
    public class ProductsController : Controller
    {
        private readonly BlockoDbContext _context;
        private readonly UserManager<User> _userManager;

        public ProductsController(BlockoDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<VendorProfile?> GetCurrentVendorProfileAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _context.VendorProfiles.FirstOrDefaultAsync(v => v.UserId == user.Id);
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string? search, int? categoryId)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var vendorId = vendor?.Id ?? 0;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Where(p => p.SupplierId == vendorId || (vendorId > 0 && p.SupplierKey == "vendor_" + vendorId))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s) || (p.NameEn != null && p.NameEn.ToLower().Contains(s)) || p.Sku.ToLower().Contains(s));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.VendorProfile = vendor;

            return View(products);
        }

        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.MaterialTypes = await _context.MaterialTypes.Where(m => m.IsActive).OrderBy(m => m.SortOrder).ToListAsync();
            return View();
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, string? dynamicSpecsJson, string? metaTitle, string? metaDescription, string? metaKeywords, decimal? tier2Discount, decimal? tier3Discount)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null)
            {
                TempData["Error"] = "يرجى إكمال وتوثيق ملف المورد الخاص بك أولاً.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (string.IsNullOrWhiteSpace(model.Name) || model.RetailPrice <= 0 || model.CategoryId <= 0)
            {
                TempData["Error"] = "يرجى تعبئة كافة الحقول الإلزامية وتحديد سعر وكمية صالحة.";
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
                ViewBag.MaterialTypes = await _context.MaterialTypes.Where(m => m.IsActive).OrderBy(m => m.SortOrder).ToListAsync();
                return View(model);
            }

            model.SupplierId = vendor.Id;
            model.SupplierKey = "vendor_" + vendor.Id;
            model.Sku = string.IsNullOrWhiteSpace(model.Sku) ? $"VND-{vendor.Id}-{DateTime.UtcNow.Ticks % 1000000}" : model.Sku;
            model.Status = ProductStatus.InStock;
            model.ModerationStatus = "PendingReview";
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dynamicSpecsJson) && dynamicSpecsJson != "{}")
            {
                model.Description = (model.Description ?? "") + (string.IsNullOrWhiteSpace(model.Description) ? "" : "\n") + "المواصفات الفنية: " + dynamicSpecsJson;
            }

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

            // Save B2B Wholesale Tier Pricing (B2B-01)
            decimal d2 = tier2Discount ?? 3.0m;
            decimal d3 = tier3Discount ?? 6.0m;

            _context.ProductTierPricings.Add(new ProductTierPricing
            {
                ProductId = model.Id,
                MinQuantity = 1,
                MaxQuantity = 10,
                UnitPrice = model.RetailPrice,
                DiscountPercentage = 0,
                TierName = "تجزئة"
            });

            _context.ProductTierPricings.Add(new ProductTierPricing
            {
                ProductId = model.Id,
                MinQuantity = 11,
                MaxQuantity = 50,
                UnitPrice = Math.Round(model.RetailPrice * (1.0m - (d2 / 100.0m)), 2),
                DiscountPercentage = d2,
                TierName = "جملة متوسطة"
            });

            _context.ProductTierPricings.Add(new ProductTierPricing
            {
                ProductId = model.Id,
                MinQuantity = 51,
                MaxQuantity = null,
                UnitPrice = Math.Round(model.RetailPrice * (1.0m - (d3 / 100.0m)), 2),
                DiscountPercentage = d3,
                TierName = "عطاءات ومشاريع كبرى"
            });

            // Save SEO Metadata
            string pageKey = $"/Product/Index/{model.Id}";
            var seo = new Bolcko.Domain.Entities.SEO.SEOMetadata
            {
                PageName = pageKey,
                PageTitle = !string.IsNullOrWhiteSpace(metaTitle) ? metaTitle.Trim() : $"{model.Name} | {vendor.CompanyNameAr}",
                MetaDescription = !string.IsNullOrWhiteSpace(metaDescription) ? metaDescription.Trim() : model.Description,
                MetaKeywords = !string.IsNullOrWhiteSpace(metaKeywords) ? metaKeywords.Trim() : $"{model.Name}, توريد مواد بناء, {vendor.CompanyNameAr}, أسعار المواد الأردن",
                PageUrl = $"/Shop/Product/Details/{model.Id}",
                LastUpdated = DateTime.UtcNow
            };
            _context.SEOMetadata.Add(seo);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة مادة البناء وشرائح أسعار الجملة بنجاح! تم إدراجها قيد مراجعة الجودة ومطابقة المواصفات الهندسية من إدارة المنصة.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var vendorId = vendor?.Id ?? 0;

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.TierPricings)
                .FirstOrDefaultAsync(p => p.Id == id && (p.SupplierId == vendorId || p.SupplierKey == "vendor_" + vendorId));

            if (product == null) return NotFound();

            string pageKey = $"/Product/Index/{product.Id}";
            var seo = await _context.SEOMetadata.FirstOrDefaultAsync(s => s.PageName == pageKey);

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.MaterialTypes = await _context.MaterialTypes.Where(m => m.IsActive).OrderBy(m => m.SortOrder).ToListAsync();
            ViewBag.SeoMetadata = seo;

            return View(product);
        }

        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, string? metaTitle, string? metaDescription, string? metaKeywords, decimal? tier2Discount, decimal? tier3Discount)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var vendorId = vendor?.Id ?? 0;

            var existing = await _context.Products
                .Include(p => p.TierPricings)
                .FirstOrDefaultAsync(p => p.Id == id && (p.SupplierId == vendorId || p.SupplierKey == "vendor_" + vendorId));
            if (existing == null) return NotFound();

            existing.Name = model.Name;
            existing.NameEn = model.NameEn;
            existing.Description = model.Description;
            existing.DescriptionEn = model.DescriptionEn;
            existing.CategoryId = model.CategoryId;
            existing.RetailPrice = model.RetailPrice;
            existing.UnitOfMeasure = model.UnitOfMeasure;
            existing.StockQuantity = model.StockQuantity;
            existing.Brand = model.Brand;
            existing.CountryOfOrigin = model.CountryOfOrigin;
            existing.IsOversized = model.IsOversized;
            existing.Status = model.Status;
            existing.ImageUrl = model.ImageUrl;
            existing.UpdatedAt = DateTime.UtcNow;

            // Update Wholesale Tier Pricings
            var existingTiers = await _context.ProductTierPricings.Where(t => t.ProductId == id).ToListAsync();
            _context.ProductTierPricings.RemoveRange(existingTiers);

            decimal d2 = tier2Discount ?? 3.0m;
            decimal d3 = tier3Discount ?? 6.0m;

            _context.ProductTierPricings.Add(new ProductTierPricing
            {
                ProductId = id,
                MinQuantity = 1,
                MaxQuantity = 10,
                UnitPrice = existing.RetailPrice,
                DiscountPercentage = 0,
                TierName = "تجزئة"
            });

            _context.ProductTierPricings.Add(new ProductTierPricing
            {
                ProductId = id,
                MinQuantity = 11,
                MaxQuantity = 50,
                UnitPrice = Math.Round(existing.RetailPrice * (1.0m - (d2 / 100.0m)), 2),
                DiscountPercentage = d2,
                TierName = "جملة متوسطة"
            });

            _context.ProductTierPricings.Add(new ProductTierPricing
            {
                ProductId = id,
                MinQuantity = 51,
                MaxQuantity = null,
                UnitPrice = Math.Round(existing.RetailPrice * (1.0m - (d3 / 100.0m)), 2),
                DiscountPercentage = d3,
                TierName = "عطاءات ومشاريع كبرى"
            });

            await _context.SaveChangesAsync();

            // Save / Update SEO Metadata
            string pageKey = $"/Product/Index/{existing.Id}";
            var seo = await _context.SEOMetadata.FirstOrDefaultAsync(s => s.PageName == pageKey);
            if (seo == null)
            {
                seo = new Bolcko.Domain.Entities.SEO.SEOMetadata
                {
                    PageName = pageKey,
                    PageTitle = !string.IsNullOrWhiteSpace(metaTitle) ? metaTitle.Trim() : $"{existing.Name} | {vendor.CompanyNameAr}",
                    MetaDescription = !string.IsNullOrWhiteSpace(metaDescription) ? metaDescription.Trim() : existing.Description,
                    MetaKeywords = !string.IsNullOrWhiteSpace(metaKeywords) ? metaKeywords.Trim() : $"{existing.Name}, توريد مواد بناء, {vendor.CompanyNameAr}, أسعار المواد الأردن",
                    PageUrl = $"/Shop/Product/Details/{existing.Id}",
                    LastUpdated = DateTime.UtcNow
                };
                _context.SEOMetadata.Add(seo);
            }
            else
            {
                seo.PageTitle = !string.IsNullOrWhiteSpace(metaTitle) ? metaTitle.Trim() : seo.PageTitle;
                seo.MetaDescription = !string.IsNullOrWhiteSpace(metaDescription) ? metaDescription.Trim() : seo.MetaDescription;
                seo.MetaKeywords = !string.IsNullOrWhiteSpace(metaKeywords) ? metaKeywords.Trim() : seo.MetaKeywords;
                seo.LastUpdated = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث مادة البناء وشرائح أسعار الجملة بنجاح!";
            return RedirectToAction(nameof(Index));
        }
    }
}
