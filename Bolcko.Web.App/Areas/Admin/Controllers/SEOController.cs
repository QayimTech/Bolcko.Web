using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.SEO.DTOs;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class SEOController : Controller
    {
        private readonly IServiceManager _serviceManager;

        public SEOController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 15)
        {
            var seoList = await _serviceManager.SEOService.GetPagedSEOAsync(page, pageSize);
            return View(seoList);
        }

        public async Task<IActionResult> Create()
        {
            return View(new SEOMetadataDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SEOMetadataDto seoDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.SEOService.AddOrUpdateSEOAsync(seoDto);
                return RedirectToAction(nameof(Index));
            }
            return View(seoDto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var seo = await _serviceManager.SEOService.GetSEOByIdAsync(id);
            if (seo == null) return NotFound();
            return View(seo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SEOMetadataDto seoDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.SEOService.AddOrUpdateSEOAsync(seoDto);
                return RedirectToAction(nameof(Index));
            }
            return View(seoDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _serviceManager.SEOService.DeleteSEOAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PopulateDefaults()
        {
            var defaults = new List<SEOMetadataDto>
            {
                new SEOMetadataDto
                {
                    PageName = "Home",
                    PageTitle = "بلوكو لتوريد مواد البناء | BLOCKO Construction Supplies",
                    MetaDescription = "منصة توريدات مواد البناء الأولى والمثالية في الأردن. أسعار يومية لحديد التسليح، الإسمنت، والخرسانة مباشرة من المصنع إلى موقعك.",
                    MetaKeywords = "مواد بناء، حديد تسليح الأردن، إسمنت، خرسانة جاهزة، توريد مشاريع، بلوكو، BLOCKO",
                    PageUrl = "/",
                    PageOrder = 1
                },
                new SEOMetadataDto
                {
                    PageName = "Products",
                    PageTitle = "تصفح كافة المنتجات ومواد البناء | BLOCKO Products",
                    MetaDescription = "تسوق أفضل العلامات التجارية لحديد التسليح، التمديدات الصحية، العدد والأدوات، ومعدات المسابح بأسعار تنافسية وجودة مضمونة.",
                    MetaKeywords = "منتجات بناء، لوازم صحية، مضخات مياه، عدد يدوية، كهربائيات، حديد، بلوكو الأردن",
                    PageUrl = "/Shop/Product",
                    PageOrder = 2
                },
                new SEOMetadataDto
                {
                    PageName = "Categories",
                    PageTitle = "مجموعات التوريد والأقسام الرئيسية | BLOCKO Categories",
                    MetaDescription = "استكشف مجموعات توريد مواد البناء والأقسام الرئيسية للعدد، الأدوات، الأدوات الصحية، والمواد اللاصقة.",
                    MetaKeywords = "أقسام مواد البناء، مستلزمات سباكة، مواد لاصقة، لوازم إنشائية",
                    PageUrl = "/Shop/Category",
                    PageOrder = 3
                },
                new SEOMetadataDto
                {
                    PageName = "Contact",
                    PageTitle = "اتصل بنا لطلب عروض الأسعار وتوريد المشاريع | Contact BLOCKO",
                    MetaDescription = "تواصل مع مستشاري توريد مواد البناء في الأردن. نحن متواجدون لمساعدتك في تسعير مشاريعك الإنشائية وتوريدها.",
                    MetaKeywords = "اتصال بلوكو، خدمة العملاء، تسعير مواد البناء، توريد خرسانة الأردن",
                    PageUrl = "/Shop/Home/Contact",
                    PageOrder = 4
                }
            };

            foreach (var page in defaults)
            {
                var existing = await _serviceManager.SEOService.GetSEOByPageNameAsync(page.PageName);
                if (existing == null)
                {
                    await _serviceManager.SEOService.AddOrUpdateSEOAsync(page);
                }
            }

            return Json(new { success = true, message = "تم تهيئة اقتراحات الـ SEO لصفحات البناء بنجاح!" });
        }

        // ===================================================================
        // DATA HUB - IMPORT / EXPORT & SEO METRICS
        // ===================================================================

        /// <summary>
        /// الصفحة الرئيسية لمركز إدارة وتصدير البيانات الشامل (Data Hub)
        /// </summary>
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ImportExport()
        {
            var (total, approved, pending, missingDesc) = await _serviceManager.ProductSeoService.GetSeoMetricsAsync();
            ViewBag.TotalProducts = total;
            ViewBag.ApprovedProducts = approved;
            ViewBag.PendingProducts = pending;
            ViewBag.MissingDescProducts = missingDesc;

            var categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            ViewBag.Categories = categories;

            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetSeoMetricsJson()
        {
            var (total, approved, pending, missingDesc) = await _serviceManager.ProductSeoService.GetSeoMetricsAsync();
            return Json(new { success = true, total, approved, pending, missingDesc });
        }

        /// <summary>
        /// تصدير شيت Excel لبيانات الـ SEO بحسب الفلاتر المحددة
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportFilteredSeo(
            int? categoryId,
            string? brand,
            string? seoStatus,
            string? search,
            bool onlyMissingDesc = false)
        {
            var filter = new Bolcko.Domain.Entities.Product.DTOs.ProductSeoFilterParamsDto
            {
                CategoryId = categoryId,
                Brand = brand,
                Search = search,
                OnlyMissingDescription = onlyMissingDesc
            };

            if (!string.IsNullOrWhiteSpace(seoStatus) &&
                Enum.TryParse<Bolcko.Domain.Enums.SeoStatus>(seoStatus, true, out var parsedStatus))
            {
                filter.SeoStatus = parsedStatus;
            }

            var fileBytes = await _serviceManager.ProductSeoService.ExportProductsSeoToExcelAsync(filter);
            var fileName = $"Product_SEO_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// تصدير شيت SEO لمنتج منفرد
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportSingleProductSeo(int productId)
        {
            var fileBytes = await _serviceManager.ProductSeoService.ExportSingleProductSeoToExcelAsync(productId);
            var fileName = $"Product_{productId}_SEO_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        /// <summary>
        /// رفع شيت استيراد SEO - معالجة فورية أو في الخلفية بـ Hangfire
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UploadSeoImport(IFormFile file, bool backgroundJob = false, bool autoApprove = true)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "الرجاء اختيار ملف Excel صالح." });

            var ext = Path.GetExtension(file.FileName)?.ToLower();
            if (ext != ".xlsx" && ext != ".xls")
                return Json(new { success = false, message = "صيغة الملف غير مدعومة. يرجى رفع ملف .xlsx فقط." });

            if (backgroundJob)
            {
                // حفظ الملف مؤقتاً وتشغيل وظيفة Hangfire في الخلفية
                var tempDir = Path.Combine(Path.GetTempPath(), "BolckoSeoImports");
                Directory.CreateDirectory(tempDir);
                var jobId = Guid.NewGuid().ToString("N");
                var tempPath = Path.Combine(tempDir, $"{jobId}.xlsx");

                using (var fs = new FileStream(tempPath, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }

                Hangfire.BackgroundJob.Enqueue(() =>
                    _serviceManager.ProductSeoService.ProcessSeoBulkImportHangfireJobAsync(jobId, tempPath, autoApprove));

                return Json(new
                {
                    success = true,
                    isAsync = true,
                    jobId = jobId,
                    message = "تم جدولة ملف الاستيراد كمهام خلفية بنجاح. يمكنك متابعة النسبة المئوية المباشرة."
                });
            }
            else
            {
                using var stream = file.OpenReadStream();
                var result = await _serviceManager.ProductSeoService.ImportProductSeoFromStreamAsync(stream, autoApprove);

                return Json(new
                {
                    success = result.IsCompleted,
                    isAsync = false,
                    totalRows = result.TotalRecords,
                    successCount = result.UpdatedCount,
                    updatedCount = result.UpdatedCount,
                    failureCount = result.Errors.Count,
                    errors = result.Errors,
                    message = result.IsCompleted ? "تمت معالجة بيانات الملف بنجاح." : "حدثت أخطاء أثناء معالجة الملف."
                });
            }
        }

        /// <summary>
        /// الاستعلام عن حالة وظيفة استيراد خلفية بـ Hangfire
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetJobStatus(string jobId)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                return Json(new { success = false, message = "رقم الوظيفة غير صحيح." });

            var result = await _serviceManager.ProductSeoService.GetSeoJobResultAsync(jobId);

            if (result == null)
            {
                return Json(new
                {
                    success = true,
                    status = "Processing",
                    progressPercent = 10,
                    message = "جاري تهيئة الوظيفة ومعالجة البيانات..."
                });
            }

            var progressPercent = result.TotalRecords > 0
                ? (int)((double)(result.UpdatedCount + result.SkippedCount + result.Errors.Count) / result.TotalRecords * 100)
                : (result.IsCompleted ? 100 : 50);

            return Json(new
            {
                success = true,
                status = result.IsCompleted ? "Completed" : "Processing",
                progressPercent = Math.Min(progressPercent, 100),
                totalRows = result.TotalRecords,
                processedRows = result.UpdatedCount + result.SkippedCount,
                successCount = result.UpdatedCount,
                updatedCount = result.UpdatedCount,
                failureCount = result.Errors.Count,
                errors = result.Errors,
                message = result.IsCompleted ? "تمت المعالجة في الخلفية بنجاح." : "جاري معالجة الصفوف في الخلفية..."
            });
        }
    }
}
