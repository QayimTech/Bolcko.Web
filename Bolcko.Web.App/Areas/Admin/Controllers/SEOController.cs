using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.SEO.DTOs;
using Bolcko.Domain.Interfaces;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class SEOController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly IBulkImportService _bulkImportService;

        public SEOController(IServiceManager serviceManager, IBulkImportService bulkImportService)
        {
            _serviceManager = serviceManager;
            _bulkImportService = bulkImportService;
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
                    PageName = "calculator",
                    PageTitle = "حاسبة تكلفة البناء وكميات المواد في الأردن 2026 | بلوكو",
                    MetaDescription = "احسب كميات وتكاليف حديد التسليح، الإسمنت، الخرسانة الجاهزة، والطوب الإسمنتي لمشروعك في الأردن بدقة هندسية وفق كودات البناء وأسعار السوق اللحظية.",
                    MetaKeywords = "حاسبة تكلفة البناء الأردن, حساب كميات الحديد, اسعار الاسمنت في الاردن, اسعار الحديد اليوم عمان, تكلفة بناء بيت عظم, اسعار الخرسانة الجاهزة, بلوكو",
                    PageUrl = "/calculator",
                    PageOrder = 2
                },
                new SEOMetadataDto
                {
                    PageName = "Products",
                    PageTitle = "تصفح كافة المنتجات ومواد البناء | BLOCKO Products",
                    MetaDescription = "تسوق أفضل العلامات التجارية لحديد التسليح، التمديدات الصحية، العدد والأدوات، ومعدات المسابح بأسعار تنافسية وجودة مضمونة.",
                    MetaKeywords = "منتجات بناء، لوازم صحية، مضخات مياه، عدد يدوية، كهربائيات، حديد، بلوكو الأردن",
                    PageUrl = "/Shop/Product",
                    PageOrder = 3
                },
                new SEOMetadataDto
                {
                    PageName = "Categories",
                    PageTitle = "مجموعات التوريد والأقسام الرئيسية | BLOCKO Categories",
                    MetaDescription = "استكشف مجموعات توريد مواد البناء والأقسام الرئيسية للعدد، الأدوات، الأدوات الصحية، والمواد اللاصقة.",
                    MetaKeywords = "أقسام مواد البناء، مستلزمات سباكة، مواد لاصقة، لوازم إنشائية",
                    PageUrl = "/Shop/Category",
                    PageOrder = 4
                },
                new SEOMetadataDto
                {
                    PageName = "About",
                    PageTitle = "من نحن | بلوكو لتوريد مواد البناء والحلول الإنشائية",
                    MetaDescription = "تعرف على منصة بلوكو، الرائدة في توريد مواد البناء والمستلزمات الإنشائية في المملكة الأردنية الهاشمية بأعلى معايير الجودة والسرعة.",
                    MetaKeywords = "عن بلوكو, شركة مواد بناء الاردن, توريد مشاريع عمان, من نحن بلوكو",
                    PageUrl = "/Shop/Home/AboutUs",
                    PageOrder = 5
                },
                new SEOMetadataDto
                {
                    PageName = "Quote",
                    PageTitle = "طلب تسعيرة وتوريد مشاريع رسمي | استدراج عروض أسعار بلوكو",
                    MetaDescription = "اطلب عرض سعر رسمي لمشروعك الإنشائي لتوريد كميات الحديد، الإسمنت، الخرسانة، ومواد التشطيب بأسعار الجملة المعتمدة.",
                    MetaKeywords = "طلب تسعيرة مواد بناء, استدراج عروض اسعار, تسعير مشاريع عظم, عروض اسعار حديد واسمنت",
                    PageUrl = "/Shop/Quote/Request",
                    PageOrder = 6
                },
                new SEOMetadataDto
                {
                    PageName = "Contact",
                    PageTitle = "اتصل بنا لطلب عروض الأسعار وتوريد المشاريع | Contact BLOCKO",
                    MetaDescription = "تواصل مع مستشاري توريد مواد البناء في الأردن. نحن متواجدون لمساعدتك في تسعير مشاريعك الإنشائية وتوريدها.",
                    MetaKeywords = "اتصال بلوكو، خدمة العملاء، تسعير مواد البناء، توريد خرسانة الأردن",
                    PageUrl = "/Shop/Home/Contact",
                    PageOrder = 7
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
        /// رفع شيت استيراد المنتجات والـ SEO - معالجة فورية أو في الخلفية بـ Hangfire
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        [RequestSizeLimit(524_288_000)] // 500 MB
        public async Task<IActionResult> UploadSeoImport(
            List<IFormFile>? files, 
            IFormFile? file, 
            IFormFile? imagesZip, 
            bool backgroundJob = false, 
            bool autoApprove = true)
        {
            var uploadedFiles = new List<IFormFile>();
            if (files != null && files.Any())
            {
                uploadedFiles.AddRange(files.Where(f => f != null && f.Length > 0));
            }
            else if (file != null && file.Length > 0)
            {
                uploadedFiles.Add(file);
            }

            if (!uploadedFiles.Any() && (imagesZip == null || imagesZip.Length == 0))
                return Json(new { success = false, message = "الرجاء اختيار ملف Excel أو ملف صور ZIP." });

            var tempDir = Path.Combine(Path.GetTempPath(), "BolckoDataHubImports", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            string? zipPath = null;
            string? extractedImagesFolder = null;

            if (imagesZip != null && imagesZip.Length > 0)
            {
                zipPath = Path.Combine(tempDir, imagesZip.FileName);
                using (var zfs = new FileStream(zipPath, FileMode.Create))
                {
                    await imagesZip.CopyToAsync(zfs);
                }

                extractedImagesFolder = Path.Combine(tempDir, "ExtractedImages");
                Directory.CreateDirectory(extractedImagesFolder);
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractedImagesFolder, true);
            }

            // ── Case 1: Images ZIP Only (no Excel) ──────────────────────────
            if (!uploadedFiles.Any())
            {
                var jobId = Guid.NewGuid().ToString("N");
                Hangfire.BackgroundJob.Enqueue<IBulkImportService>(service =>
                    service.ProcessImagesZipImportJobAsync(jobId, extractedImagesFolder ?? zipPath!));

                return Json(new
                {
                    success = true,
                    isAsync = true,
                    jobId = jobId,
                    message = "تم بدء معالجة وربط حزمة الصور بالمنتجات في الخلفية."
                });
            }

            // ── Case 2: Excel Files (Single or Multiple) ───────────────────
            var validExcelPaths = new List<string>();
            foreach (var uploadedFile in uploadedFiles)
            {
                var ext = Path.GetExtension(uploadedFile.FileName)?.ToLower();
                if (ext != ".xlsx" && ext != ".xls")
                    continue;

                var excelPath = Path.Combine(tempDir, uploadedFile.FileName);
                using (var fs = new FileStream(excelPath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(fs);
                }
                validExcelPaths.Add(excelPath);
            }

            if (!validExcelPaths.Any())
                return Json(new { success = false, message = "صيغة الملف غير مدعومة. يرجى رفع ملفات .xlsx فقط." });

            if (backgroundJob)
            {
                string firstJobId = Guid.NewGuid().ToString("N");
                for (int i = 0; i < validExcelPaths.Count; i++)
                {
                    var path = validExcelPaths[i];
                    var jobId = i == 0 ? firstJobId : Guid.NewGuid().ToString("N");
                    Hangfire.BackgroundJob.Enqueue<IBulkImportService>(service =>
                        service.ProcessUnifiedExcelImportJobAsync(jobId, path, extractedImagesFolder ?? zipPath));
                }

                string msg = validExcelPaths.Count == 1
                    ? "تم جدولة ملف الاستيراد كمهام خلفية بنجاح. يمكنك متابعة تقدم المعالجة."
                    : $"تم جدولة {validExcelPaths.Count} ملفات إكسل للمعالجة بالتتابع في طابور الخلفية بنجاح.";

                return Json(new
                {
                    success = true,
                    isAsync = true,
                    jobId = firstJobId,
                    totalJobs = validExcelPaths.Count,
                    message = msg
                });
            }
            else
            {
                int totalRows = 0, imported = 0, updated = 0, skipped = 0;
                var allErrors = new List<string>();
                bool hasAnyError = false;

                foreach (var excelPath in validExcelPaths)
                {
                    var result = await _bulkImportService.ProcessUnifiedExcelImportAsync(excelPath, extractedImagesFolder);
                    totalRows += result.TotalRows;
                    imported += result.Imported;
                    updated += result.Updated;
                    skipped += result.Skipped;

                    var errorRows = result.Rows
                        .Where(r => r.Status == Bolcko.Domain.Interfaces.ImportRowStatus.Skipped && !string.IsNullOrWhiteSpace(r.Reason))
                        .Select(r => $"({Path.GetFileName(excelPath)}) الصف {r.RowNumber}: {r.Reason}")
                        .ToList();

                    allErrors.AddRange(errorRows);
                    if (result.HasError) hasAnyError = true;
                }

                return Json(new
                {
                    success = !hasAnyError,
                    isAsync = false,
                    totalRows = totalRows,
                    successCount = imported,
                    updatedCount = updated + imported,
                    failureCount = skipped,
                    errors = allErrors,
                    message = !hasAnyError 
                        ? $"تمت معالجة {validExcelPaths.Count} ملفات بنجاح: {imported} منتج/فارينت جديد، وتحديث {updated}." 
                        : "حدثت أخطاء أثناء المعالجة."
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

        /// <summary>
        /// تحميل نموذج إكسل قياسي متكامل لاستيراد المنتجات مع الفارينتس والـ SEO
        /// </summary>
        [HttpGet]
        public IActionResult DownloadProductImportTemplate()
        {
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var ws = workbook.Worksheets.Add("Products & Variants");
            ws.RightToLeft = true;

            var headers = new[]
            {
                "كود المنتج (SKU)", "اسم المنتج (عربي)", "اسم المنتج (English)", "التصنيف الرئيسي", "التصنيف الفرعي",
                "أيقونة الفئة", "الوصف (عربي)", "الوصف (English)", "السعر (Retail Price)", "الكمية (Stock)",
                "وحدة القياس", "البراند", "بلد المنشأ", "بضاعة ضخمة (Oversized)", "الصورة (Image URL)",
                "كود المنتج الأساسي (Parent SKU)", "كود الفارينت (Variant SKU)", "المقاس / الحجم", "اللون", "سعر الفارينت",
                "مخزون الفارينت", "صورة الفارينت", "عنوان SEO", "وصف SEO", "الكلمات المفتاحية SEO"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1E293B");
                cell.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                cell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
            }

            // Sample Row 1: Standalone Product (No Variants)
            ws.Cell(2, 1).Value = "PRD-CEMENT-50KG";
            ws.Cell(2, 2).Value = "إسمنت بورتلاندي 50 كغم";
            ws.Cell(2, 3).Value = "Portland Cement 50kg";
            ws.Cell(2, 4).Value = "مواد بناء";
            ws.Cell(2, 5).Value = "إسمنت وجير";
            ws.Cell(2, 6).Value = "construction";
            ws.Cell(2, 7).Value = "إسمنت عالي الجودة للبناء والتشييد مطابق للمواصفات الأردنية";
            ws.Cell(2, 8).Value = "High quality cement for general construction purposes";
            ws.Cell(2, 9).Value = 6.50;
            ws.Cell(2, 10).Value = 500;
            ws.Cell(2, 11).Value = "كيس";
            ws.Cell(2, 12).Value = "المناصير";
            ws.Cell(2, 13).Value = "الأردن";
            ws.Cell(2, 14).Value = "نعم";
            ws.Cell(2, 15).Value = "https://images.unsplash.com/photo-1589939705384-5185137a7f0f";
            ws.Cell(2, 16).Value = "";
            ws.Cell(2, 17).Value = "";
            ws.Cell(2, 18).Value = "";
            ws.Cell(2, 19).Value = "";
            ws.Cell(2, 20).Value = "";
            ws.Cell(2, 21).Value = "";
            ws.Cell(2, 22).Value = "";
            ws.Cell(2, 23).Value = "شراء إسمنت بورتلاندي 50 كغم بأفضل سعر في الأردن | بلوكو";
            ws.Cell(2, 24).Value = "اطلب إسمنت بورتلاندي 50 كغم مع خدمة التوصيل لموقع البناء مباشرة عبر منصة بلوكو.";
            ws.Cell(2, 25).Value = "إسمنت, مواد بناء, إسمنت بورتلاندي, بلوكو, أسعار الإسمنت";

            // Sample Row 2: Parent Product with Variants
            ws.Cell(3, 1).Value = "PRD-PAINT-EMULSION";
            ws.Cell(3, 2).Value = "دهان داخلي مائي فاخر";
            ws.Cell(3, 3).Value = "Premium Interior Emulsion Paint";
            ws.Cell(3, 4).Value = "دهانات ومستلزماتها";
            ws.Cell(3, 5).Value = "دهانات داخلية";
            ws.Cell(3, 6).Value = "format_paint";
            ws.Cell(3, 7).Value = "دهان مائي قابل للغسيل بتغطية ممتازة ومقاومة للبقع";
            ws.Cell(3, 8).Value = "Washable emulsion paint with superior coverage";
            ws.Cell(3, 9).Value = 25.00;
            ws.Cell(3, 10).Value = 100;
            ws.Cell(3, 11).Value = "جالون";
            ws.Cell(3, 12).Value = "جوتن";
            ws.Cell(3, 13).Value = "الإمارات";
            ws.Cell(3, 14).Value = "لا";
            ws.Cell(3, 15).Value = "https://images.unsplash.com/photo-1562259949-e8e7689d7828";
            ws.Cell(3, 16).Value = "";
            ws.Cell(3, 17).Value = "";
            ws.Cell(3, 18).Value = "";
            ws.Cell(3, 19).Value = "";
            ws.Cell(3, 20).Value = "";
            ws.Cell(3, 21).Value = "";
            ws.Cell(3, 22).Value = "";
            ws.Cell(3, 23).Value = "دهان داخلي مائي فاخر من جوتن | متجر بلوكو";
            ws.Cell(3, 24).Value = "تسوق أفضل الدهانات الداخلية المائية بتشكيلة ألوان وأحجام متعددة مع توصيل سريع.";
            ws.Cell(3, 25).Value = "دهانات, جوتن, دهان مائي, تشطيبات داخلية";

            // Sample Row 3: Child Variant 1 (White 1 Gallon)
            ws.Cell(4, 1).Value = "";
            ws.Cell(4, 2).Value = "دهان داخلي مائي فاخر - أبيض (1 جالون)";
            ws.Cell(4, 3).Value = "Premium Emulsion - White (1 Gallon)";
            ws.Cell(4, 4).Value = "دهانات ومستلزماتها";
            ws.Cell(4, 5).Value = "دهانات داخلية";
            ws.Cell(4, 6).Value = "";
            ws.Cell(4, 7).Value = "";
            ws.Cell(4, 8).Value = "";
            ws.Cell(4, 9).Value = 25.00;
            ws.Cell(4, 10).Value = 60;
            ws.Cell(4, 11).Value = "جالون";
            ws.Cell(4, 12).Value = "جوتن";
            ws.Cell(4, 13).Value = "الإمارات";
            ws.Cell(4, 14).Value = "لا";
            ws.Cell(4, 15).Value = "";
            ws.Cell(4, 16).Value = "PRD-PAINT-EMULSION"; // Links to Parent SKU
            ws.Cell(4, 17).Value = "PRD-PAINT-WHT-1G";   // Variant SKU
            ws.Cell(4, 18).Value = "1 جالون (3.6 لتر)";
            ws.Cell(4, 19).Value = "أبيض نصفي #FFFFFF";
            ws.Cell(4, 20).Value = 25.00;
            ws.Cell(4, 21).Value = 60;
            ws.Cell(4, 22).Value = "https://images.unsplash.com/photo-1562259949-e8e7689d7828";
            ws.Cell(4, 23).Value = "";
            ws.Cell(4, 24).Value = "";
            ws.Cell(4, 25).Value = "";

            // Sample Row 4: Child Variant 2 (White 18L Drum)
            ws.Cell(5, 1).Value = "";
            ws.Cell(5, 2).Value = "دهان داخلي مائي فاخر - أبيض (برميل 18 لتر)";
            ws.Cell(5, 3).Value = "Premium Emulsion - White (18L Drum)";
            ws.Cell(5, 4).Value = "دهانات ومستلزماتها";
            ws.Cell(5, 5).Value = "دهانات داخلية";
            ws.Cell(5, 6).Value = "";
            ws.Cell(5, 7).Value = "";
            ws.Cell(5, 8).Value = "";
            ws.Cell(5, 9).Value = 85.00;
            ws.Cell(5, 10).Value = 40;
            ws.Cell(5, 11).Value = "برميل";
            ws.Cell(5, 12).Value = "جوتن";
            ws.Cell(5, 13).Value = "الإمارات";
            ws.Cell(5, 14).Value = "نعم";
            ws.Cell(5, 15).Value = "";
            ws.Cell(5, 16).Value = "PRD-PAINT-EMULSION"; // Links to Parent SKU
            ws.Cell(5, 17).Value = "PRD-PAINT-WHT-18L";  // Variant SKU
            ws.Cell(5, 18).Value = "برميل (18 لتر)";
            ws.Cell(5, 19).Value = "أبيض نصفي #FFFFFF";
            ws.Cell(5, 20).Value = 85.00;
            ws.Cell(5, 21).Value = 40;
            ws.Cell(5, 22).Value = "https://images.unsplash.com/photo-1589939705384-5185137a7f0f";
            ws.Cell(5, 23).Value = "";
            ws.Cell(5, 24).Value = "";
            ws.Cell(5, 25).Value = "";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Bolcko_Products_Master_Template_{DateTime.UtcNow:yyyyMMdd}.xlsx");
        }
    }
}
