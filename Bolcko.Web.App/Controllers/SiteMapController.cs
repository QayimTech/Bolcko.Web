using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;
using System.Xml.Linq;

namespace Bolcko.Web.App.Controllers
{
    /// <summary>
    /// Controller for generating dynamic XML Sitemap for SEO
    /// </summary>
    public class SiteMapController : Controller
    {
        private readonly IServiceManager _serviceManager;
        private readonly ILogger<SiteMapController> _logger;

        public SiteMapController(IServiceManager serviceManager, ILogger<SiteMapController> logger)
        {
            _serviceManager = serviceManager;
            _logger = logger;
        }

        /// <summary>
        /// Generates and returns the XML Sitemap
        /// URL: /sitemap.xml
        /// </summary>
        [Route("sitemap.xml")]
        [Produces("application/xml")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var baseUrl = GetBaseUrl();
                var urls = new List<SitemapUrl>();

                // إضافة الصفحات الثابتة (Static Pages)
                AddStaticPages(urls, baseUrl);

                // إضافة الصفحات الديناميكية (Dynamic Pages)
                await AddDynamicPages(urls, baseUrl);

                // إنشاء XML Sitemap
                var sitemapXml = GenerateSitemapXml(urls);

                return Content(sitemapXml, "application/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sitemap");
                return StatusCode(500, "Error generating sitemap");
            }
        }

        /// <summary>
        /// إضافة الصفحات الثابتة
        /// </summary>
        private void AddStaticPages(List<SitemapUrl> urls, string baseUrl)
        {
            // الصفحة الرئيسية
            urls.Add(new SitemapUrl
            {
                Loc = baseUrl,
                LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ChangeFreq = "daily",
                Priority = "1.0"
            });

            // صفحات Shop Area
            urls.Add(new SitemapUrl
            {
                Loc = $"{baseUrl}/Shop/Home/AboutUs",
                LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ChangeFreq = "monthly",
                Priority = "0.8"
            });

            urls.Add(new SitemapUrl
            {
                Loc = $"{baseUrl}/Shop/Home/Contact",
                LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ChangeFreq = "monthly",
                Priority = "0.8"
            });

            urls.Add(new SitemapUrl
            {
                Loc = $"{baseUrl}/Shop/Home/Privacy",
                LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ChangeFreq = "monthly",
                Priority = "0.5"
            });

            // صفحة فئات المنتجات الرئيسية
            urls.Add(new SitemapUrl
            {
                Loc = $"{baseUrl}/Shop/Category",
                LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ChangeFreq = "weekly",
                Priority = "0.9"
            });
        }

        /// <summary>
        /// إضافة الصفحات الديناميكية (جداول SEOMetadata، الفئات، والمنتجات)
        /// </summary>
        private async Task AddDynamicPages(List<SitemapUrl> urls, string baseUrl)
        {
            try
            {
                var existingUrls = new HashSet<string>(urls.Select(u => u.Loc.TrimEnd('/')), StringComparer.OrdinalIgnoreCase);

                // 1. جلب كافة الصفحات المعرفة في جدول SEOMetadata ديناميكياً من قاعدة البيانات
                var seoPages = await _serviceManager.SEOService.GetAllSEOAsync();
                foreach (var page in seoPages)
                {
                    if (string.IsNullOrWhiteSpace(page.PageUrl)) continue;

                    var urlPath = page.PageUrl.Trim();
                    var fullUrl = urlPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? urlPath
                        : $"{baseUrl.TrimEnd('/')}/{(urlPath.StartsWith("/") ? urlPath.TrimStart('/') : urlPath)}";

                    var normalized = fullUrl.TrimEnd('/');
                    if (!existingUrls.Contains(normalized))
                    {
                        var priority = "0.8";
                        if (page.PageName.Equals("calculator", StringComparison.OrdinalIgnoreCase) ||
                            page.PageName.Equals("home", StringComparison.OrdinalIgnoreCase))
                        {
                            priority = "0.9";
                        }

                        urls.Add(new SitemapUrl
                        {
                            Loc = fullUrl,
                            LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                            ChangeFreq = "weekly",
                            Priority = priority
                        });
                        existingUrls.Add(normalized);
                    }
                }

                // 2. جلب جميع الفئات
                var categories = await _serviceManager.CategoryService.GetAllCategoriesAsync();
                foreach (var category in categories)
                {
                    var catUrl = $"{baseUrl}/Shop/Category/Index/{category.Id}";
                    if (!existingUrls.Contains(catUrl.TrimEnd('/')))
                    {
                        urls.Add(new SitemapUrl
                        {
                            Loc = catUrl,
                            LastMod = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                            ChangeFreq = "weekly",
                            Priority = "0.8"
                        });
                        existingUrls.Add(catUrl.TrimEnd('/'));
                    }
                }

                // 3. جلب جميع المنتجات
                var products = await _serviceManager.ProductService.GetAllProductsAsync();
                foreach (var product in products)
                {
                    var prodUrl = $"{baseUrl}/Shop/Product/Index/{product.Id}";
                    if (!existingUrls.Contains(prodUrl.TrimEnd('/')))
                    {
                        urls.Add(new SitemapUrl
                        {
                            Loc = prodUrl,
                            LastMod = product.UpdatedAt.ToString("yyyy-MM-dd"),
                            ChangeFreq = "weekly",
                            Priority = "0.7"
                        });
                        existingUrls.Add(prodUrl.TrimEnd('/'));
                    }
                }

                _logger.LogInformation("Sitemap: Added {SeoCount} SEO pages, {CategoryCount} categories and {ProductCount} products", 
                    seoPages.Count(), categories.Count(), products.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding dynamic pages to sitemap");
            }
        }

        /// <summary>
        /// إنشاء XML Sitemap
        /// </summary>
        private string GenerateSitemapXml(List<SitemapUrl> urls)
        {
            var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
            
            var root = new XElement(ns + "urlset");
            
            foreach (var url in urls)
            {
                var urlElement = new XElement(ns + "url",
                    new XElement(ns + "loc", url.Loc),
                    new XElement(ns + "lastmod", url.LastMod),
                    new XElement(ns + "changefreq", url.ChangeFreq),
                    new XElement(ns + "priority", url.Priority)
                );
                
                root.Add(urlElement);
            }
            
            var document = new XDocument(root);
            return document.ToString();
        }

        /// <summary>
        /// الحصول على Base URL للموقع
        /// </summary>
        private string GetBaseUrl()
        {
            var request = HttpContext.Request;
            return $"https://{request.Host}";
        }
    }

    /// <summary>
    /// نموذج URL في Sitemap
    /// </summary>
    public class SitemapUrl
    {
        public string Loc { get; set; } = string.Empty;
        public string LastMod { get; set; } = string.Empty;
        public string ChangeFreq { get; set; } = "weekly";
        public string Priority { get; set; } = "0.5";
    }
}
