using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.Product;
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
    [Authorize(Roles = "Admin, DashboardUser")]
    public class ProductController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ITranslationService _translationService;

        public ProductController(
            IServiceManager serviceManager,
            IWebHostEnvironment webHostEnvironment,
            IBackgroundJobClient backgroundJobClient,
            ITranslationService translationService)
        {
            _serviceManager = serviceManager;
            _webHostEnvironment = webHostEnvironment;
            _backgroundJobClient = backgroundJobClient;
            _translationService = translationService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var products = await _serviceManager.ProductService.GetPagedProductsAsync(page, pageSize);
            var viewModel = new ProductIndexViewModel
            {
                Products = products
            };
            return View(viewModel);
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
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    
                    int order = 1;
                    foreach (var image in uploadImages)
                    {
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }
                        productDto.Images.Add(new ProductImageDto { Url = "/images/products/" + uniqueFileName, DisplayOrder = order++ });
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
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                        
                        int order = 1;
                        foreach (var image in uploadImages)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await image.CopyToAsync(fileStream);
                            }
                            productDto.Images.Add(new ProductImageDto { Url = "/images/products/" + uniqueFileName, DisplayOrder = order++ });
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
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "لا يمكن حذف هذا المنتج لأنه مرتبط بطلبات سابقة أو سلات تسوق لعملاء، الرجاء أرشفته بدلاً من ذلك.";
            }
            return RedirectToAction(nameof(Index));
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
        [Authorize(Roles = "Admin")]
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
    }
}
