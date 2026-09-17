using Blocko.Persistence;
using Bolcko.Domain.Entities.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Bolcko.Web.App.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [Route("Admin/[controller]")]
    public class MaterialTypesController : Controller
    {
        private readonly BlockoDbContext _context;

        public MaterialTypesController(BlockoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var materials = await _context.MaterialTypes
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.NameAr)
                .ToListAsync();
            return View(materials);
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaterialType model)
        {
            if (string.IsNullOrWhiteSpace(model.NameAr) || string.IsNullOrWhiteSpace(model.CategoryType))
            {
                TempData["Error"] = "يرجى إدخال اسم المادة ونوع التصنيف الإنشائي.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.Slug))
            {
                model.Slug = (model.NameEn ?? model.NameAr).ToLower().Replace(" ", "-");
            }

            if (string.IsNullOrWhiteSpace(model.SpecSchemaJson))
            {
                model.SpecSchemaJson = "{}";
            }

            model.CreatedAt = DateTime.UtcNow;
            _context.MaterialTypes.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تمت إضافة مادة البناء وتصنيفها الديناميكي بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MaterialType model)
        {
            var existing = await _context.MaterialTypes.FindAsync(model.Id);
            if (existing == null) return NotFound();

            existing.NameAr = model.NameAr;
            existing.NameEn = model.NameEn;
            existing.CategoryType = model.CategoryType;
            existing.UnitOfMeasure = model.UnitOfMeasure;
            existing.DefaultPriceEstimated = model.DefaultPriceEstimated;
            existing.SpecSchemaJson = model.SpecSchemaJson ?? "{}";
            existing.IsActive = model.IsActive;
            existing.SortOrder = model.SortOrder;

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم تحديث مواصفات مادة البناء بنجاح!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Route("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _context.MaterialTypes.FindAsync(id);
            if (existing != null)
            {
                _context.MaterialTypes.Remove(existing);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم حذف المادة بنجاح.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("GetSchema/{id}")]
        public async Task<IActionResult> GetSchema(int id)
        {
            var mat = await _context.MaterialTypes.FindAsync(id);
            if (mat == null) return NotFound();
            return Json(new
            {
                id = mat.Id,
                nameAr = mat.NameAr,
                categoryType = mat.CategoryType,
                unitOfMeasure = mat.UnitOfMeasure,
                defaultPrice = mat.DefaultPriceEstimated,
                schema = mat.SpecSchemaJson
            });
        }
    }
}
