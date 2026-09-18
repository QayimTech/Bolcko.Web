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
        public async Task<IActionResult> Create(Product model, string? dynamicSpecsJson, string? metaTitle, string? metaDescription, string? metaKeywords)
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
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(dynamicSpecsJson) && dynamicSpecsJson != "{}")
            {
                model.Description = (model.Description ?? "") + (string.IsNullOrWhiteSpace(model.Description) ? "" : "\n") + "المواصفات الفنية: " + dynamicSpecsJson;
            }

            _context.Products.Add(model);
            await _context.SaveChangesAsync();

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

            TempData["Success"] = "تمت إضافة مادة البناء وتكوين محرك الـ SEO بنجاح!";
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
        public async Task<IActionResult> Edit(int id, Product model, string? metaTitle, string? metaDescription, string? metaKeywords)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var vendorId = vendor?.Id ?? 0;

            var existing = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && (p.SupplierId == vendorId || p.SupplierKey == "vendor_" + vendorId));
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

            await _context.SaveChangesAsync();

            // Save / Update SEO Metadata
            string pageKey = $"/Product/Index/{existing.Id}";
            var seo = await _context.SEOMetadata.FirstOrDefaultAsync(s => s.PageName == pageKey);
            if (seo == null)
            {
                seo = new Bolcko.Domain.Entities.SEO.SEOMetadata
                {
                    PageName = pageKey,
                    PageTitle = !string.IsNullOrWhiteSpace(metaTitle) ? metaTitle.Trim() : $"{existing.Name} | {vendor?.CompanyNameAr ?? "BLOCKO"}",
                    MetaDescription = !string.IsNullOrWhiteSpace(metaDescription) ? metaDescription.Trim() : existing.Description,
                    MetaKeywords = metaKeywords?.Trim(),
                    PageUrl = $"/Shop/Product/Details/{existing.Id}",
                    LastUpdated = DateTime.UtcNow
                };
                _context.SEOMetadata.Add(seo);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(metaTitle)) seo.PageTitle = metaTitle.Trim();
                if (!string.IsNullOrWhiteSpace(metaDescription)) seo.MetaDescription = metaDescription.Trim();
                if (!string.IsNullOrWhiteSpace(metaKeywords)) seo.MetaKeywords = metaKeywords.Trim();
                seo.LastUpdated = DateTime.UtcNow;
                _context.SEOMetadata.Update(seo);
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم تحديث بيانات المنتج وإعدادات محرك الـ SEO بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// استعراض وتخصيص بضائع مزود 'القناص' لضمها إلى حساب المورد وتحديد هوامش الربح
        /// </summary>
        [HttpGet]
        [Route("ClaimQannas")]
        public async Task<IActionResult> ClaimQannas(string? search)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.SupplierKey == "qannas" || p.SupplierId == null || p.SupplierId == 0)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s) || (p.NameEn != null && p.NameEn.ToLower().Contains(s)));
            }

            var qannasProducts = await query.OrderByDescending(p => p.Id).Take(50).ToListAsync();
            ViewBag.VendorProfile = vendor;
            return View(qannasProducts);
        }

        [HttpPost]
        [Route("ClaimProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimProduct(int productId, decimal customPrice, int customStock)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null)
            {
                return Json(new { success = false, message = "يرجى توثيق حساب المورد أولاً." });
            }

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "المنتج غير موجود." });
            }

            // Assign product to vendor with updated pricing
            product.SupplierId = vendor.Id;
            product.SupplierKey = "vendor_" + vendor.Id;
            if (customPrice > 0) product.RetailPrice = customPrice;
            if (customStock > 0) product.StockQuantity = customStock;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "تم ضم المنتج إلى متجرك بنجاح وتحديث أسعار التوريد!" });
        }
    }
}
