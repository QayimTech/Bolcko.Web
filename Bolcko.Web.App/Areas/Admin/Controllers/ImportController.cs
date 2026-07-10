using Bolcko.Domain.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class ImportController : Controller
    {
        private readonly IBackgroundJobClient _jobs;
        private readonly IWebHostEnvironment  _env;
        private readonly IBulkImportService   _importService;

        public ImportController(
            IBackgroundJobClient jobs,
            IWebHostEnvironment  env,
            IBulkImportService   importService)
        {
            _jobs          = jobs;
            _env           = env;
            _importService = importService;
        }

        // ─── GET: /Admin/Import/BulkImport ──────────────────────────────────
        [HttpGet]
        public IActionResult BulkImport()
        {
            return View();
        }

        // ─── POST: /Admin/Import/BulkImport ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(104_857_600)] // 100 MB
        public async Task<IActionResult> BulkImport(IFormFile file, string format = "excel")
        {
            if (file == null || file.Length == 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "الرجاء اختيار ملف للرفع." });

                TempData["ErrorMessage"] = "الرجاء اختيار ملف للرفع.";
                return RedirectToAction(nameof(BulkImport));
            }

            bool isJson  = format.Equals("json",  StringComparison.OrdinalIgnoreCase);
            bool isExcel = format.Equals("excel", StringComparison.OrdinalIgnoreCase);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (isJson && ext != ".json")
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "عند اختيار JSON يجب أن يكون الملف بصيغة .json" });

                TempData["ErrorMessage"] = "عند اختيار JSON يجب أن يكون الملف بصيغة .json";
                return RedirectToAction(nameof(BulkImport));
            }

            if (isExcel && ext != ".xlsx")
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "عند اختيار Excel يجب أن يكون الملف بصيغة .xlsx" });

                TempData["ErrorMessage"] = "عند اختيار Excel يجب أن يكون الملف بصيغة .xlsx";
                return RedirectToAction(nameof(BulkImport));
            }

            // Save to temp folder
            var tempFolder = Path.Combine(_env.ContentRootPath, "App_Data", "Imports");
            Directory.CreateDirectory(tempFolder);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(tempFolder, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            // ── Excel: run inline so we get a detailed result back ───────────
            if (isExcel)
            {
                ImportResult result;
                try
                {
                    result = await _importService.ProcessUnifiedExcelImportAsync(filePath);
                }
                catch (Exception ex)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = $"خطأ أثناء الاستيراد: {ex.Message}" });

                    TempData["ErrorMessage"] = $"❌ خطأ أثناء الاستيراد: {ex.Message}";
                    return RedirectToAction(nameof(BulkImport));
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = !result.HasError,
                        message = result.Summary,
                        result  = new
                        {
                            totalRows = result.TotalRows,
                            imported  = result.Imported,
                            updated   = result.Updated,
                            skipped   = result.Skipped,
                            rows      = result.Rows
                                .Where(r => r.Status != ImportRowStatus.Imported)
                                .Select(r => new
                                {
                                    rowNumber = r.RowNumber,
                                    name      = r.Name,
                                    status    = r.Status.ToString(),
                                    reason    = r.Reason
                                })
                        }
                    });
                }

                if (result.HasError)
                {
                    TempData["ErrorMessage"] = $"❌ {result.Summary}";
                }
                else
                {
                    TempData["SuccessMessage"] = $"✅ {result.Summary}";
                }
                return RedirectToAction(nameof(BulkImport));
            }

            // ── JSON: keep as background job (returns immediately) ───────────
            string jobId = _jobs.Enqueue<IBulkImportService>(svc => svc.ProcessUnifiedJsonImportAsync(filePath));
            var msg      = $"تم استلام ملف JSON وبدأت المعالجة في الخلفية (Job #{jobId}). تابع من شاشة المهام.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = msg, result = (object?)null });

            TempData["SuccessMessage"] = $"✅ {msg}";
            return RedirectToAction(nameof(BulkImport));
        }

        // ─── GET: /Admin/Import/DownloadTemplate?format=excel ───────────────
        [HttpGet]
        public IActionResult DownloadTemplate(string format = "excel")
        {
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
                return DownloadJsonTemplate();

            return DownloadExcelTemplate();
        }

        // ────────────────────────────────────────────────────────────────────
        private IActionResult DownloadExcelTemplate()
        {
            using var wb = new ClosedXML.Excel.XLWorkbook();

            // ── Sheet: Products ─────────────────────────────────────────────
            var prodWs = wb.Worksheets.Add("المنتجات");
            prodWs.Cell(1,  1).Value = "الصورة";
            prodWs.Cell(1,  2).Value = "اسم المنتج (عربي)";
            prodWs.Cell(1,  3).Value = "Product Name (English)";
            prodWs.Cell(1,  4).Value = "البراند";
            prodWs.Cell(1,  5).Value = "اسم المورد";
            prodWs.Cell(1,  6).Value = "بلد المنشأ";
            prodWs.Cell(1,  7).Value = "الوصف (العربي)";
            prodWs.Cell(1,  8).Value = "Description (English)";
            prodWs.Cell(1,  9).Value = "التصنيف الرئيسي";
            prodWs.Cell(1, 10).Value = "التصنيف الفرعي";
            prodWs.Cell(1, 11).Value = "أيقونة الفئة";
            prodWs.Cell(1, 12).Value = "السعر";
            prodWs.Cell(1, 13).Value = "الكمية";
            prodWs.Cell(1, 14).Value = "الوحدة";
            prodWs.Cell(1, 15).Value = "الوزن";
            prodWs.Cell(1, 16).Value = "المقاس (Meters)";
            prodWs.Cell(1, 17).Value = "كود المنتج";
            prodWs.Cell(1, 18).Value = "صلاح المنتج";

            // Sample row
            prodWs.Cell(2,  1).Value = "← أضف صورة";
            prodWs.Cell(2,  2).Value = "آيفون 15";
            prodWs.Cell(2,  3).Value = "iPhone 15";
            prodWs.Cell(2,  4).Value = "Apple";
            prodWs.Cell(2,  5).Value = "مورد الإلكترونيات";
            prodWs.Cell(2,  6).Value = "China";
            prodWs.Cell(2,  7).Value = "هاتف آيفون 15 الأصلي";
            prodWs.Cell(2,  8).Value = "Original iPhone 15";
            prodWs.Cell(2,  9).Value = "هواتف";
            prodWs.Cell(2, 10).Value = "إلكترونيات";
            prodWs.Cell(2, 11).Value = "smartphone";
            prodWs.Cell(2, 12).Value = 3999.00;
            prodWs.Cell(2, 13).Value = 50;
            prodWs.Cell(2, 14).Value = "قطعة";
            prodWs.Cell(2, 15).Value = 0.174;
            prodWs.Cell(2, 16).Value = "14.6 x 7.1 x 0.78 cm";
            prodWs.Cell(2, 17).Value = "IPHONE15-001";
            prodWs.Cell(2, 18).Value = "Active";

            StyleHeader(prodWs, 18);

            prodWs.Cell(4, 1).Value = "ملاحظة: إذا تركت كود المنتج فارغاً سيتم توليده تلقائياً. اسم المنتج (عربي) والسعر والتصنيف الرئيسي إلزامية.";
            prodWs.Cell(4, 1).Style.Font.Italic = true;
            prodWs.Cell(4, 1).Style.Font.FontColor = ClosedXML.Excel.XLColor.Gray;

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Bolcko_Import_Template.xlsx");
        }

        private IActionResult DownloadJsonTemplate()
        {
            const string template = """
{
  "categories": [
    {
      "name": "إلكترونيات",
      "parentCategoryName": "",
      "description": "أجهزة إلكترونية",
      "displayOrder": 1
    },
    {
      "name": "هواتف",
      "parentCategoryName": "إلكترونيات",
      "description": "هواتف ذكية",
      "displayOrder": 2
    }
  ],
  "products": [
    {
      "name": "iPhone 15",
      "nameEn": "iPhone 15",
      "categoryName": "هواتف",
      "brand": "Apple",
      "countryOfOrigin": "China",
      "price": 3999.00,
      "description": "هاتف آيفون 15 الأصلي",
      "stock": 50,
      "unitOfMeasure": "قطعة",
      "status": "Active",
      "imageBase64": "data:image/jpeg;base64,/9j/4AAQSkZJRgAB..."
    }
  ]
}
""";
            var bytes = System.Text.Encoding.UTF8.GetBytes(template);
            return File(bytes, "application/json", "Bolcko_Import_Template.json");
        }

        private static void StyleHeader(ClosedXML.Excel.IXLWorksheet ws, int colCount)
        {
            var range = ws.Range(1, 1, 1, colCount);
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1e293b");
            range.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
            range.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            ws.Columns().AdjustToContents();
        }
    }
}
