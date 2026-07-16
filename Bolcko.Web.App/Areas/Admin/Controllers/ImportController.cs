using Bolcko.Domain.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
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
        private readonly ILogger<ImportController> _logger;

        public ImportController(
            IBackgroundJobClient jobs,
            IWebHostEnvironment  env,
            IBulkImportService   importService,
            ILogger<ImportController> logger)
        {
            _jobs          = jobs;
            _env           = env;
            _importService = importService;
            _logger = logger;
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
        [RequestSizeLimit(524_288_000)] // 500 MB
        public async Task<IActionResult> BulkImport(IFormFile? file, IFormFile? imagesZip, string format = "excel", string googleSheetUrl = "", string localImageFolder = "")
        {
            _logger.LogInformation("=== BulkImport background start ===");
            _logger.LogInformation("Format: {Format}", format);
            
            bool isJson  = format.Equals("json",  StringComparison.OrdinalIgnoreCase);
            bool isExcel = format.Equals("excel", StringComparison.OrdinalIgnoreCase);
            bool isGoogleSheet = format.Equals("google-sheet", StringComparison.OrdinalIgnoreCase);
            
            var importId = Guid.NewGuid().ToString();
            string? tempZipPath = null;
            string? tempFilePath = null;

            try
            {
                // Create imports directory if not exists
                var tempFolder = Path.Combine(_env.ContentRootPath, "App_Data", "Imports");
                Directory.CreateDirectory(tempFolder);

                // Save ZIP file if provided
                if (imagesZip != null && imagesZip.Length > 0)
                {
                    _logger.LogInformation("Saving images ZIP to temp folder");
                    tempZipPath = Path.Combine(tempFolder, $"{importId}_images.zip");
                    await using (var stream = new FileStream(tempZipPath, FileMode.Create))
                    {
                        await imagesZip.CopyToAsync(stream);
                    }
                }

                if (isGoogleSheet)
                {
                    if (string.IsNullOrWhiteSpace(googleSheetUrl))
                    {
                        return Json(new { success = false, message = "الرجاء إدخال رابط Google Sheet." });
                    }

                    // Enqueue Google Sheet import job
                    _jobs.Enqueue<IBulkImportService>(svc => svc.ProcessGoogleSheetImportJobAsync(importId, googleSheetUrl, tempZipPath));
                    return Json(new { success = true, message = "بدأت المعالجة في الخلفية لشيت جوجل.", jobId = importId });
                }

                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "الرجاء اختيار ملف للرفع." });
                }

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".xlsx")
                {
                    return Json(new { success = false, message = "الرجاء رفع ملف بصيغة Excel (.xlsx) فقط." });
                }

                // Save data file
                tempFilePath = Path.Combine(tempFolder, $"{importId}_data{ext}");
                await using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _jobs.Enqueue<IBulkImportService>(svc => svc.ProcessUnifiedExcelImportJobAsync(importId, tempFilePath, tempZipPath));
                return Json(new { success = true, message = "بدأت معالجة ملف Excel في الخلفية.", jobId = importId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start background import job");
                return Json(new { success = false, message = $"فشل بدء المعالجة: {ex.Message}" });
            }
        }

        // ─── GET: /Admin/Import/GetImportStatus?jobId=xxx ───────────────────
        [HttpGet]
        public IActionResult GetImportStatus(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return Json(new { success = false, message = "معرف المهمة غير صالح" });
            }

            // Use ContentRootPath to match the path used by the Hangfire background job writer
            var resultsFolder = Path.Combine(_env.ContentRootPath, "App_Data", "Imports", "Results");
            var resultFilePath = Path.Combine(resultsFolder, $"{jobId}.json");

            if (System.IO.File.Exists(resultFilePath))
            {
                try
                {
                    var jsonContent = System.IO.File.ReadAllText(resultFilePath);
                    var result = System.Text.Json.JsonSerializer.Deserialize<ImportResult>(jsonContent);
                    
                    // Cleanup results file to prevent file accumulation
                    try { System.IO.File.Delete(resultFilePath); } catch { }

                    return Json(new
                    {
                        success = true,
                        completed = true,
                        message = result?.Summary,
                        result = new
                        {
                            totalRows = result?.TotalRows ?? 0,
                            imported = result?.Imported ?? 0,
                            updated = result?.Updated ?? 0,
                            skipped = result?.Skipped ?? 0,
                            rows = (result?.Rows ?? new List<ImportRowResult>())
                                .Where(r => r.Status != ImportRowStatus.Imported)
                                .Select(r => new
                                {
                                    rowNumber = r.RowNumber,
                                    name = r.Name,
                                    status = r.Status.ToString(),
                                    reason = r.Reason
                                })
                        }
                    });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = $"خطأ في قراءة النتيجة: {ex.Message}" });
                }
            }

            return Json(new { success = true, completed = false, message = "جاري المعالجة في الخلفية..." });
        }

        // ─── GET: /Admin/Import/DownloadTemplate ────────────────────────────
        [HttpGet]
        public IActionResult DownloadTemplate()
        {
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

        // DownloadJsonTemplate removed to reduce module over-engineering.

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
