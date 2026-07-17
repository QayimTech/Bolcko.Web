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
            _logger.LogInformation("=== BulkImport request received. Format={Format}, FileSize={FileSize}, ZipSize={ZipSize} ===",
                format,
                file?.Length ?? 0,
                imagesZip?.Length ?? 0);

            bool isGoogleSheet = format.Equals("google-sheet", StringComparison.OrdinalIgnoreCase);

            var importId = Guid.NewGuid().ToString();

            // Base folder: ContentRootPath ensures the same physical path is seen
            // by both this web process and the Hangfire in-process worker.
            var importFolder  = Path.Combine(_env.ContentRootPath, "App_Data", "Imports");
            Directory.CreateDirectory(importFolder);

            string? tempFilePath          = null;
            string? extractedImagesFolder = null;

            try
            {
                // ── 1. Extract ZIP immediately (in this request) ────────────────
                // We MUST extract here, not inside the Hangfire job, because on
                // cloud hosts (Render) the container can hibernate between the
                // enqueue and the job execution, leaving only DB rows — not files.
                if (imagesZip != null && imagesZip.Length > 0)
                {
                    extractedImagesFolder = Path.Combine(importFolder, "Extracted", importId);
                    Directory.CreateDirectory(extractedImagesFolder);

                    _logger.LogInformation("Extracting images ZIP ({Size} bytes) to {Folder}",
                        imagesZip.Length, extractedImagesFolder);

                    await using var zipStream = imagesZip.OpenReadStream();
                    using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

                    int extracted = 0;
                    foreach (var entry in archive.Entries)
                    {
                        // Skip directories and hidden/system files
                        if (string.IsNullOrWhiteSpace(entry.Name) || entry.Name.StartsWith('.'))
                            continue;

                        var fileName = Path.GetFileName(entry.FullName);
                        if (string.IsNullOrWhiteSpace(fileName)) continue;

                        var destPath = Path.Combine(extractedImagesFolder, fileName);

                        // Handle duplicate filenames inside the ZIP
                        if (System.IO.File.Exists(destPath))
                        {
                            var baseName = Path.GetFileNameWithoutExtension(fileName);
                            var ext2     = Path.GetExtension(fileName);
                            int ctr      = 1;
                            while (System.IO.File.Exists(destPath))
                                destPath = Path.Combine(extractedImagesFolder, $"{baseName}_{ctr++}{ext2}");
                        }

                        await using var entryStream = entry.Open();
                        await using var fileStream  = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                        await entryStream.CopyToAsync(fileStream);
                        extracted++;
                    }

                    _logger.LogInformation("ZIP extraction complete: {Count} images extracted", extracted);
                }

                // ── 2. Handle Google Sheet ──────────────────────────────────────
                if (isGoogleSheet)
                {
                    if (string.IsNullOrWhiteSpace(googleSheetUrl))
                        return Json(new { success = false, message = "الرجاء إدخال رابط Google Sheet." });

                    _jobs.Enqueue<IBulkImportService>(svc =>
                        svc.ProcessGoogleSheetImportJobAsync(importId, googleSheetUrl, extractedImagesFolder));

                    return Json(new { success = true, message = "بدأت المعالجة في الخلفية لشيت جوجل.", jobId = importId });
                }

                // ── 3. Handle Excel upload ──────────────────────────────────────
                if (file == null || file.Length == 0)
                    return Json(new { success = false, message = "الرجاء اختيار ملف للرفع." });

                var fileExt = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (fileExt != ".xlsx")
                    return Json(new { success = false, message = "الرجاء رفع ملف بصيغة Excel (.xlsx) فقط." });

                tempFilePath = Path.Combine(importFolder, $"{importId}_data{fileExt}");
                await using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation("Excel file saved to {Path}. Enqueueing background job.", tempFilePath);

                // Pass extractedImagesFolder (not the raw ZIP path) so the job
                // uses already-extracted images — no re-extraction needed in the job.
                _jobs.Enqueue<IBulkImportService>(svc =>
                    svc.ProcessUnifiedExcelImportJobAsync(importId, tempFilePath, extractedImagesFolder));

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
