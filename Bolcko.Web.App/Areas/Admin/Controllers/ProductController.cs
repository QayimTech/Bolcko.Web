using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Image;
using Bolcko.Domain.Entities.Product.DTOs;
using Bolcko.Web.App.Areas.Admin.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using Hangfire;
using Bolcko.Domain.Interfaces;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin,DashboardUser")]
    public class ProductController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ITranslationService _translationService;
        private readonly IImageService _imageService;

        public ProductController(
            IServiceManager serviceManager,
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            IBackgroundJobClient backgroundJobClient,
            ITranslationService translationService,
            IImageService imageService)
        {
            _serviceManager = serviceManager;
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _backgroundJobClient = backgroundJobClient;
            _translationService = translationService;
            _imageService = imageService;
        }

        public async Task<IActionResult> Index(
            int page = 1, 
            int pageSize = 10, 
            string? search = null, 
            int? categoryId = null, 
            int? supplierId = null,
            string? moderationStatus = null,
            string? sortOrder = null)
        {
            var query = _unitOfWork.Products
                .GetAllAsQueryable(trackChanges: false)
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s) || 
                                         (p.NameEn != null && p.NameEn.ToLower().Contains(s)) ||
                                         p.Sku.ToLower().Contains(s));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (supplierId.HasValue && supplierId.Value > 0)
            {
                query = query.Where(p => p.SupplierId == supplierId.Value || p.SupplierKey == "vendor_" + supplierId.Value);
            }

            if (!string.IsNullOrWhiteSpace(moderationStatus) && moderationStatus != "All")
            {
                query = query.Where(p => p.ModerationStatus == moderationStatus);
            }

            var totalCount = await query.CountAsync();

            IOrderedQueryable<Bolcko.Domain.Entities.Product.Product> ordered = sortOrder switch
            {
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "price_asc" => query.OrderBy(p => p.RetailPrice),
                "price_desc" => query.OrderByDescending(p => p.RetailPrice),
                _ => query.OrderByDescending(p => p.Id)
            };

            var items = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vendorProfiles = await _unitOfWork.VendorProfiles.GetAllAsQueryable(trackChanges: false).ToListAsync();
            var vendorDict = vendorProfiles.ToDictionary(v => v.Id, v => v.CompanyNameAr);

            var productDtos = items.Select(p => {
                string supplierName = "(بدون مورد)";
                int vId = 0;
                if (p.SupplierId.HasValue && vendorDict.ContainsKey(p.SupplierId.Value))
                {
                    supplierName = vendorDict[p.SupplierId.Value];
                }
                else if (!string.IsNullOrEmpty(p.SupplierKey) && p.SupplierKey.StartsWith("vendor_") && int.TryParse(p.SupplierKey.Replace("vendor_", ""), out vId) && vendorDict.ContainsKey(vId))
                {
                    supplierName = vendorDict[vId];
                }

                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    NameEn = p.NameEn,
                    Description = p.Description,
                    DescriptionEn = p.DescriptionEn,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category?.Name,
                    SupplierId = p.SupplierId,
                    SupplierName = supplierName,
                    RetailPrice = p.RetailPrice,
                    StockQuantity = p.StockQuantity,
                    UnitOfMeasure = p.UnitOfMeasure,
                    Sku = p.Sku,
                    ImageUrl = p.ImageUrl,
                    ModerationStatus = string.IsNullOrEmpty(p.ModerationStatus) ? "Approved" : p.ModerationStatus,
                    RejectionReason = p.RejectionReason,
                    ModeratedAt = p.ModeratedAt,
                    UpdatedAt = p.UpdatedAt
                };
            }).ToList();

            var pagedList = new Blocko.Persistence.Common.PagedList<ProductDto>(productDtos, totalCount, page, pageSize);
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            ViewBag.Vendors = vendorProfiles;

            var viewModel = new ProductIndexViewModel
            {
                Products = pagedList,
                Search = search,
                CategoryId = categoryId,
                SupplierId = supplierId,
                ModerationStatus = moderationStatus,
                SortOrder = sortOrder
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product != null)
            {
                product.ModerationStatus = "Approved";
                product.RejectionReason = null;
                product.ModeratedAt = DateTime.UtcNow;
                _unitOfWork.Products.Update(product);
                await _unitOfWork.CompleteAsync();
                TempData["SuccessMessage"] = $"تم اعتماد ونشر مادة البناء '{product.Name}' بنجاح! ✓";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? reason)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product != null)
            {
                product.ModerationStatus = "Rejected";
                product.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "لم يستوفِ معايير الجودة ومطابقة الجمعية العلمية الملكية (RSS/ASTM)" : reason.Trim();
                product.ModeratedAt = DateTime.UtcNow;
                _unitOfWork.Products.Update(product);
                await _unitOfWork.CompleteAsync();
                TempData["SuccessMessage"] = $"تم رفض مادة البناء '{product.Name}' وتوثيق السبب للمورد.";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            var newProduct = new ProductDto
            {
                Sku = "BLK-" + DateTime.UtcNow.ToString("yyMMddHHmmss")
            };
            return View(newProduct);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDto productDto, List<IFormFile> uploadImages)
        {
            if (ModelState.IsValid)
            {
                if (uploadImages != null && uploadImages.Count > 0)
                {
                    int order = 1;
                    foreach (var image in uploadImages)
                    {
                        using (var stream = image.OpenReadStream())
                        {
                            string savedPath = await _imageService.SaveImageAsync(stream, image.FileName, "images/products");
                            if (!string.IsNullOrEmpty(savedPath))
                            {
                                productDto.Images.Add(new ProductImageDto { Url = "/" + savedPath.Replace("\\", "/").TrimStart('/'), DisplayOrder = order++ });
                            }
                        }
                    }
                    if(productDto.Images.Any())
                    {
                        productDto.ImageUrl = productDto.Images.First().Url;
                    }
                }

                await _serviceManager.ProductService.AddProductAsync(productDto);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(productDto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _serviceManager.ProductService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductDto productDto, List<IFormFile> uploadImages, List<int>? deleteImageIds)
        {
            Serilog.Log.Information("[POST Edit] Received product {Id} with {Count} variants in request.", productDto.Id, productDto.Variants?.Count ?? 0);
            if (productDto.Variants != null)
            {
                foreach (var v in productDto.Variants)
                {
                    Serilog.Log.Information("[POST Edit] Bound Variant ID: {Id}, Sku: {Sku}, Size: {Size}, Color: {Color}, Price: {Price}, Stock: {Stock}, Img: {Img}", 
                        v.Id, v.Sku, v.Size, v.Color, v.Price, v.StockQuantity, v.ImageUrl);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Delete physical files from disk for deleted images
                    if (deleteImageIds != null && deleteImageIds.Any())
                    {
                        var existingProduct = await _serviceManager.ProductService.GetProductByIdAsync(productDto.Id);
                        if (existingProduct != null && existingProduct.Images != null)
                        {
                            foreach (var id in deleteImageIds)
                            {
                                var img = existingProduct.Images.FirstOrDefault(i => i.Id == id);
                                if (img != null && !string.IsNullOrEmpty(img.Url))
                                {
                                    try
                                    {
                                        string relativePath = img.Url.Replace("/", "\\").TrimStart('\\');
                                        string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                                        if (System.IO.File.Exists(fullPath))
                                        {
                                            System.IO.File.Delete(fullPath);
                                        }
                                    }
                                    catch (Exception) { /* Ignore file access/deletion errors */ }
                                }
                            }
                        }
                    }

                    if (uploadImages != null && uploadImages.Count > 0)
                    {
                        int order = 1;
                        foreach (var image in uploadImages)
                        {
                            using (var stream = image.OpenReadStream())
                            {
                                string savedPath = await _imageService.SaveImageAsync(stream, image.FileName, "images/products");
                                if (!string.IsNullOrEmpty(savedPath))
                                {
                                    productDto.Images.Add(new ProductImageDto { Url = "/" + savedPath.Replace("\\", "/").TrimStart('/'), DisplayOrder = order++ });
                                }
                            }
                        }
                        if(string.IsNullOrEmpty(productDto.ImageUrl) && productDto.Images.Any())
                        {
                            productDto.ImageUrl = productDto.Images.First().Url;
                        }
                    }

                    await _serviceManager.ProductService.UpdateProductAsync(productDto, deleteImageIds);
                    TempData["SuccessMessage"] = "تم تحديث المنتج بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء التحديث: " + ex.Message;
                }
            }
            ViewBag.Categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
            return View(productDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _serviceManager.ProductService.GetProductByIdAsync(id);
                if (product != null)
                {
                    if (product.Images != null && product.Images.Any())
                    {
                        foreach (var img in product.Images)
                        {
                            try
                            {
                                string relativePath = img.Url.Replace("/", "\\").TrimStart('\\');
                                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                                if (System.IO.File.Exists(fullPath))
                                {
                                    System.IO.File.Delete(fullPath);
                                }
                            }
                            catch (Exception) { /* Ignore */ }
                        }
                    }
                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        try
                        {
                            string relativePath = product.ImageUrl.Replace("/", "\\").TrimStart('\\');
                            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch (Exception) { /* Ignore */ }
                    }
                }

                await _serviceManager.ProductService.DeleteProductAsync(id);
                TempData["SuccessMessage"] = "تم حذف المنتج بنجاح.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا المنتج لأنه مرتبط بطلبات سابقة أو سلات تسوق لعملاء، الرجاء أرشفته بدلاً من ذلك.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ─── AJAX: Upload a single variant image ───────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVariantImage(IFormFile variantImage)
        {
            if (variantImage == null || variantImage.Length == 0)
                return Json(new { success = false, message = "لم يتم اختيار صورة" });

            if (variantImage.Length > 5 * 1024 * 1024)
                return Json(new { success = false, message = "حجم الصورة يتجاوز 5 ميجابايت" });

            if (!variantImage.ContentType.StartsWith("image/"))
                return Json(new { success = false, message = "الملف المختار ليس صورة" });

            try
            {
                string url = "";
                using (var stream = variantImage.OpenReadStream())
                {
                    string savedPath = await _imageService.SaveImageAsync(stream, variantImage.FileName, "images/variants");
                    if (!string.IsNullOrEmpty(savedPath))
                    {
                        url = "/" + savedPath.Replace("\\", "/").TrimStart('/');
                    }
                    else
                    {
                        return Json(new { success = false, message = "فشل ضغط ومعالجة الصورة" });
                    }
                }
                return Json(new { success = true, url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطأ في رفع الصورة: " + ex.Message });
            }
        }

        [HttpGet]
        [AllowAnonymous] // Allow quick access for diagnostics
        public async Task<IActionResult> DebugProduct(int id)
        {
            var product = await _serviceManager.ProductService.GetProductByIdAsync(id);
            if (product == null) return Content($"Product with ID {id} not found.");
            
            return Json(new {
                Id = product.Id,
                Name = product.Name,
                VariantsCount = product.Variants?.Count ?? 0,
                Variants = product.Variants
            });
        }

        // Redirect old /Product/BulkImport links to the unified import page
        [HttpGet]
        public IActionResult BulkImport()
            => RedirectToAction("BulkImport", "Import", new { area = "Admin" });

        /// <summary>
        /// AJAX endpoint — translates all products where NameEn is empty.
        /// Called by the "ترجم الكل" button in the product index view.
        /// Runs synchronously (may take a few minutes for 900 products) and returns JSON stats.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> BulkTranslate()
        {
            try
            {
                // 1. ترجمة المنتجات
                var (pTranslated, pSkipped, pFailed) = await _serviceManager.ProductService
                    .BulkTranslateAsync(_translationService);

                // 2. ترجمة الفئات
                var (cTranslated, cSkipped, cFailed) = await _serviceManager.CategoryService
                    .BulkTranslateCategoriesAsync(_translationService);

                int totalTranslated = pTranslated + cTranslated;
                int totalSkipped = pSkipped + cSkipped;
                int totalFailed = pFailed + cFailed;

                return Json(new
                {
                    success = true,
                    translated = totalTranslated,
                    skipped = totalSkipped,
                    failed = totalFailed,
                    message = $"تمت الترجمة بنجاح: {pTranslated} منتج و {cTranslated} فئة. (تخطي: {totalSkipped}، فشل: {totalFailed})"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "حدث خطأ: " + ex.Message });
            }
        }

        /// <summary>
        /// Debug endpoint - Tests translation of a single word to diagnose API connectivity.
        /// Access via: /Admin/Product/TestTranslate
        /// REMOVE IN PRODUCTION after debugging.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> TestTranslate(string word = "صوف صخري")
        {
            var results = new System.Text.StringBuilder();
            results.AppendLine($"Testing translation of: '{word}'");
            results.AppendLine("---");

            // Test MyMemory
            try
            {
                var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string url = $"https://api.mymemory.translated.net/get?q={Uri.EscapeDataString(word)}&langpair=ar|en";
                results.AppendLine($"MyMemory URL: {url}");
                var resp = await client.GetAsync(url);
                string body = await resp.Content.ReadAsStringAsync();
                results.AppendLine($"MyMemory Status: {resp.StatusCode}");
                results.AppendLine($"MyMemory Response: {body}");
            }
            catch (Exception ex)
            {
                results.AppendLine($"MyMemory FAILED: {ex.GetType().Name} - {ex.Message}");
            }

            results.AppendLine("---");

            // Test Google
            try
            {
                var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl=en&dt=t&q={Uri.EscapeDataString(word)}";
                results.AppendLine($"Google URL: {url}");
                var resp = await client.GetAsync(url);
                string body = await resp.Content.ReadAsStringAsync();
                results.AppendLine($"Google Status: {resp.StatusCode}");
                results.AppendLine($"Google Response: {body}");
            }
            catch (Exception ex)
            {
                results.AppendLine($"Google FAILED: {ex.GetType().Name} - {ex.Message}");
            }

            results.AppendLine("---");

            // Test Google Mobile HTML Scraper
            try
            {
                var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPad; CPU OS 15_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.6 Mobile/15E148 Safari/604.1");
                string url = $"https://translate.google.com/m?hl=en&sl=ar&q={Uri.EscapeDataString(word)}";
                results.AppendLine($"Google Mobile URL: {url}");
                var resp = await client.GetAsync(url);
                string html = await resp.Content.ReadAsStringAsync();
                
                string marker = "class=\"result-container\">";
                int index = html.IndexOf(marker);
                if (index != -1)
                {
                    int start = index + marker.Length;
                    int end = html.IndexOf("</div>", start);
                    if (end != -1)
                    {
                        string trans = html.Substring(start, end - start);
                        trans = System.Net.WebUtility.HtmlDecode(trans).Trim();
                        results.AppendLine($"Google Mobile Translated: '{trans}'");
                    }
                    else
                    {
                        results.AppendLine("Google Mobile: found container but no closing div");
                    }
                }
                else
                {
                    results.AppendLine("Google Mobile: result-container not found in HTML response");
                }
            }
            catch (Exception ex)
            {
                results.AppendLine($"Google Mobile FAILED: {ex.GetType().Name} - {ex.Message}");
            }

            return Content(results.ToString(), "text/plain");
        }
    }
}
