using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using Bolcko.Domain.Entities.Catalog.DTOs;
using Bolcko.Web.App.Areas.Admin.Models.ViewModels;
using Hangfire;
using Bolcko.Domain.Interfaces;
using System.IO;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, DashboardUser")]
    public class CategoryController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _webHostEnvironment;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IBulkImportService _bulkImportService;

        public CategoryController(
            IServiceManager serviceManager,
            Microsoft.AspNetCore.Hosting.IWebHostEnvironment webHostEnvironment,
            IBackgroundJobClient backgroundJobClient,
            IBulkImportService bulkImportService)
        {
            _serviceManager = serviceManager;
            _webHostEnvironment = webHostEnvironment;
            _backgroundJobClient = backgroundJobClient;
            _bulkImportService = bulkImportService;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var categories = await _serviceManager.CategoryService.GetPagedCategoriesAsync(page, pageSize);
            var viewModel = new CategoryIndexViewModel
            {
                Categories = categories
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(new CategoryDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryDto categoryDto)
        {
            if (ModelState.IsValid)
            {
                await _serviceManager.CategoryService.AddCategoryAsync(categoryDto);
                return RedirectToAction(nameof(Index));
            }
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(categoryDto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var category = await _serviceManager.CategoryService.GetCategoryByIdAsync(id);
            if (category == null) return NotFound();
            
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryDto categoryDto)
        {
            if (ModelState.IsValid)
            {
                try 
                {
                    await _serviceManager.CategoryService.UpdateCategoryAsync(categoryDto);
                    TempData["SuccessMessage"] = "تم تحديث الفئة بنجاح.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "حدث خطأ أثناء التحديث: " + ex.Message;
                }
            }
            ViewBag.ParentCategories = await _serviceManager.CategoryService.GetRootCategoriesAsync();
            return View(categoryDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _serviceManager.CategoryService.DeleteCategoryAsync(id);
                TempData["SuccessMessage"] = "Category deleted successfully.";
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to delete category.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Categories");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Name";
                worksheet.Cell(1, 2).Value = "ParentCategoryName";
                worksheet.Cell(1, 3).Value = "Description";
                worksheet.Cell(1, 4).Value = "DisplayOrder";

                // Example row
                worksheet.Cell(2, 1).Value = "Electronics";
                worksheet.Cell(2, 2).Value = ""; 
                worksheet.Cell(2, 3).Value = "Electronic items and gadgets";
                worksheet.Cell(2, 4).Value = 1;

                // Style headers
                worksheet.Range("A1:D1").Style.Font.Bold = true;
                worksheet.Range("A1:D1").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                worksheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Category_Import_Template.xlsx");
                }
            }
        }

        // Redirect old /Category/BulkImport links to the unified import page
        [HttpGet]
        public IActionResult BulkImport()
            => RedirectToAction("BulkImport", "Import", new { area = "Admin" });


        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(52428800)] // 50 MB
        public async Task<IActionResult> BulkImport(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "الرجاء اختيار ملف Excel للرفع.";
                return View();
            }

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "يدعم النظام ملفات .xlsx فقط.";
                return View();
            }

            var tempFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "App_Data", "Imports");
            Directory.CreateDirectory(tempFolder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(tempFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var jobId = _backgroundJobClient.Enqueue<IBulkImportService>(svc => svc.ProcessCategoryImportAsync(filePath));

            TempData["SuccessMessage"] = $"✅ تم بدء عملية الاستيراد (Job ID: {jobId}). الفئات ستُضاف في الخلفية. يمكنك المتابعة على /hangfire";
            return RedirectToAction(nameof(Index));
        }
    }
}
