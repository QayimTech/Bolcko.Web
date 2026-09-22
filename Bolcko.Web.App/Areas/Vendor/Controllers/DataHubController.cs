using Blocko.Persistence;
using Bolcko.Domain.Entities.Catalog;
using Bolcko.Domain.Entities.Product;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
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
    public class DataHubController : Controller
    {
        private readonly BlockoDbContext _context;
        private readonly UserManager<User> _userManager;

        public DataHubController(BlockoDbContext context, UserManager<User> userManager)
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
        public async Task<IActionResult> Index()
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var vendorId = vendor?.Id ?? 0;

            var vendorProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SupplierId == vendorId || (vendorId > 0 && p.SupplierKey == "vendor_" + vendorId))
                .ToListAsync();

            ViewBag.VendorProfile = vendor;
            ViewBag.TotalProducts = vendorProducts.Count;
            ViewBag.InStockProducts = vendorProducts.Count(p => p.StockQuantity > 0);
            ViewBag.LowStockProducts = vendorProducts.Count(p => p.StockQuantity <= 5 && p.StockQuantity > 0);
            ViewBag.OutOfStockProducts = vendorProducts.Count(p => p.StockQuantity == 0);
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

            return View(vendorProducts.Take(15).ToList());
        }

        [HttpGet]
        [Route("DownloadTemplate")]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("قائمة المنتجات");
            ws.RightToLeft = true;

            // Headers
            var headers = new[]
            {
                "كود المادة (SKU)",
                "اسم المنتج بالعربي *",
                "اسم المنتج بالإنجليزي",
                "التصنيف *",
                "سعر البيع (د.أ) *",
                "الكمية المتوفرة *",
                "وحدة القياس",
                "العلامة التجارية / المصنع",
                "بلد المنشأ",
                "الوصف والمواصفات الفنية"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8A020");
                cell.Style.Font.FontColor = XLColor.FromHtml("#0F172A");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Sample Rows
            var sampleRows = new List<object[]>
            {
                new object[] { "BLK-CON-20", "طوب إسمنتي مفرغ 20 سم نخب أول", "Hollow Concrete Block 20cm", "مواد البناء الأساسية", 0.45, 10000, "حبة", "مصنع القنّاص", "الأردن", "طوب إسمنتي عالي التحمل مطابق للمواصفات الأردنية JSMO" },
                new object[] { "STL-REBAR-12", "حديد تسليح مشوه قطر 12 ملم - شد 60", "Deformed Steel Rebar 12mm Grade 60", "الحديد والصلب", 580.00, 150, "طن", "الحديد الأردني", "الأردن", "حديد تسليح عالي المقاومة معتمد للمشاريع الإنشائية" },
                new object[] { "CEM-PORT-50", "إسمنت بورتلاندي عادي 50 كغم", "Ordinary Portland Cement 50kg", "الإسمنت والخلطات", 4.20, 2500, "كيس", "لافارج الأردن", "الأردن", "إسمنت مطابق لمعيار EN 197-1 CEM I 42.5N" }
            };

            for (int r = 0; r < sampleRows.Count; r++)
            {
                for (int c = 0; c < sampleRows[r].Length; c++)
                {
                    ws.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(sampleRows[r][c]);
                }
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Blocko_Vendor_Catalog_Template_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        [HttpPost]
        [Route("Upload")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile? catalogFile)
        {
            var vendor = await GetCurrentVendorProfileAsync();
            if (vendor == null)
            {
                TempData["Error"] = "يرجى توثيق ملف المورد قبل استخدام مركز استيراد البيانات.";
                return RedirectToAction(nameof(Index));
            }

            if (catalogFile == null || catalogFile.Length == 0)
            {
                TempData["Error"] = "يرجى اختيار ملف Excel بصيغة .xlsx صالحة.";
                return RedirectToAction(nameof(Index));
            }

            var extension = Path.GetExtension(catalogFile.FileName).ToLowerInvariant();
            if (extension != ".xlsx" && extension != ".xls")
            {
                TempData["Error"] = "صيغة الملف غير مدعومة. يرجى رفع ملف Excel (.xlsx) فقط.";
                return RedirectToAction(nameof(Index));
            }

            int addedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            try
            {
                using var stream = catalogFile.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    TempData["Error"] = "ملف الإكسل لا يحتوي على أية أوراق عمل صالحة.";
                    return RedirectToAction(nameof(Index));
                }

                var rows = worksheet.RangeUsed()?.RowsUsed()?.Skip(1); // Skip header
                if (rows == null || !rows.Any())
                {
                    TempData["Error"] = "ملف الإكسل فارغ ولا يحتوي على صفوف بيانات.";
                    return RedirectToAction(nameof(Index));
                }

                var categories = await _context.Categories.ToListAsync();
                var defaultCategory = categories.FirstOrDefault(c => c.Name.Contains("بناء") || c.Name.Contains("مواد")) ?? categories.FirstOrDefault();

                foreach (var row in rows)
                {
                    var sku = row.Cell(1).GetString()?.Trim();
                    var nameAr = row.Cell(2).GetString()?.Trim();
                    var nameEn = row.Cell(3).GetString()?.Trim();
                    var catName = row.Cell(4).GetString()?.Trim();
                    
                    if (string.IsNullOrWhiteSpace(nameAr))
                    {
                        skippedCount++;
                        continue;
                    }

                    decimal.TryParse(row.Cell(5).GetString()?.Trim(), out var retailPrice);
                    int.TryParse(row.Cell(6).GetString()?.Trim(), out var stockQty);
                    var unit = row.Cell(7).GetString()?.Trim();
                    var brand = row.Cell(8).GetString()?.Trim();
                    var origin = row.Cell(9).GetString()?.Trim();
                    var description = row.Cell(10).GetString()?.Trim();

                    if (retailPrice <= 0)
                    {
                        retailPrice = 1.00m;
                    }

                    // Resolve Category
                    Category? targetCat = null;
                    if (!string.IsNullOrWhiteSpace(catName))
                    {
                        targetCat = categories.FirstOrDefault(c => c.Name.Equals(catName, StringComparison.OrdinalIgnoreCase) ||
                                                                  (c.NameEn != null && c.NameEn.Equals(catName, StringComparison.OrdinalIgnoreCase)));
                    }
                    targetCat ??= defaultCategory;

                    if (targetCat == null)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Auto SKU if missing
                    if (string.IsNullOrWhiteSpace(sku))
                    {
                        sku = $"VND-{vendor.Id}-{DateTime.UtcNow.Ticks % 1000000}-{addedCount + 1}";
                    }

                    // Strict Multi-Tenant Lookup: Match by SKU AND (SupplierId == vendor.Id OR SupplierKey == "vendor_" + vendor.Id)
                    var existingProduct = await _context.Products
                        .FirstOrDefaultAsync(p => p.Sku == sku && (p.SupplierId == vendor.Id || p.SupplierKey == "vendor_" + vendor.Id));

                    if (existingProduct != null)
                    {
                        // Update existing product owned by this vendor
                        existingProduct.Name = nameAr;
                        if (!string.IsNullOrWhiteSpace(nameEn)) existingProduct.NameEn = nameEn;
                        existingProduct.RetailPrice = retailPrice;
                        existingProduct.StockQuantity = stockQty;
                        if (!string.IsNullOrWhiteSpace(unit)) existingProduct.UnitOfMeasure = unit;
                        if (!string.IsNullOrWhiteSpace(brand)) existingProduct.Brand = brand;
                        if (!string.IsNullOrWhiteSpace(origin)) existingProduct.CountryOfOrigin = origin;
                        if (!string.IsNullOrWhiteSpace(description)) existingProduct.Description = description;
                        existingProduct.CategoryId = targetCat.Id;
                        existingProduct.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                    else
                    {
                        // Create new product stamped with vendor ID
                        var newProduct = new Product
                        {
                            Sku = sku,
                            Name = nameAr,
                            NameEn = nameEn,
                            RetailPrice = retailPrice,
                            StockQuantity = stockQty,
                            UnitOfMeasure = !string.IsNullOrWhiteSpace(unit) ? unit : "حبة",
                            Brand = !string.IsNullOrWhiteSpace(brand) ? brand : vendor.CompanyNameAr,
                            CountryOfOrigin = !string.IsNullOrWhiteSpace(origin) ? origin : "الأردن",
                            Description = description,
                            CategoryId = targetCat.Id,
                            SupplierId = vendor.Id,
                            SupplierKey = "vendor_" + vendor.Id,
                            Status = stockQty > 0 ? ProductStatus.InStock : ProductStatus.OutOfStock,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.Products.Add(newProduct);
                        addedCount++;
                    }
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = $"تمت معالجة ملف الاستيراد بنجاح! تم إضافة ({addedCount}) منتج جديد وتحديث ({updatedCount}) منتج حالي.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء معالجة الملف: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("Export")]
        public async Task<IActionResult> Export()
        {
            var vendor = await GetCurrentVendorProfileAsync();
            var vendorId = vendor?.Id ?? 0;

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.SupplierId == vendorId || (vendorId > 0 && p.SupplierKey == "vendor_" + vendorId))
                .OrderBy(p => p.CategoryId)
                .ThenBy(p => p.Name)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("كتالوج المورد");
            ws.RightToLeft = true;

            var headers = new[]
            {
                "كود المادة (SKU)",
                "اسم المنتج بالعربي",
                "اسم المنتج بالإنجليزي",
                "التصنيف",
                "سعر البيع (د.أ)",
                "الكمية المتوفرة",
                "وحدة القياس",
                "العلامة التجارية",
                "بلد المنشأ",
                "حالة التوفر",
                "تاريخ التحديث"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
                cell.Style.Font.FontColor = XLColor.FromHtml("#FFFFFF");
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int rowIdx = 2;
            foreach (var p in products)
            {
                ws.Cell(rowIdx, 1).Value = p.Sku;
                ws.Cell(rowIdx, 2).Value = p.Name;
                ws.Cell(rowIdx, 3).Value = p.NameEn ?? "";
                ws.Cell(rowIdx, 4).Value = p.Category?.Name ?? "";
                ws.Cell(rowIdx, 5).Value = p.RetailPrice;
                ws.Cell(rowIdx, 6).Value = p.StockQuantity;
                ws.Cell(rowIdx, 7).Value = !string.IsNullOrWhiteSpace(p.UnitOfMeasure) ? p.UnitOfMeasure : "حبة";
                ws.Cell(rowIdx, 8).Value = p.Brand ?? "";
                ws.Cell(rowIdx, 9).Value = p.CountryOfOrigin ?? "الأردن";
                ws.Cell(rowIdx, 10).Value = p.StockQuantity > 0 ? "متوفر" : "نفذ من المخزون";
                ws.Cell(rowIdx, 11).Value = p.UpdatedAt.ToString("yyyy-MM-dd HH:mm");
                rowIdx++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            var fileName = $"Blocko_Vendor_{vendor?.CompanyNameAr ?? "Catalog"}_Export_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
