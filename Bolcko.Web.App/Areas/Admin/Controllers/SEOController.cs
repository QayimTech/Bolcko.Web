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
    }
}
