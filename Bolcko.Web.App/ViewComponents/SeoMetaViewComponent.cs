using Microsoft.AspNetCore.Mvc;
using Blocko.Services.Interfaces;

namespace Bolcko.Web.App.ViewComponents
{
    public class SeoMetaViewComponent : ViewComponent
    {
        private readonly IServiceManager _serviceManager;

        public SeoMetaViewComponent(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(string? defaultTitle = null)
        {
            var path = HttpContext.Request.Path.Value ?? "/";
            var query = HttpContext.Request.QueryString.Value ?? string.Empty;
            var fullPathWithQuery = $"{path}{query}";
            var isArabic = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("ar");
            
            // 1. Dynamic Search in Database by exact PageUrl (e.g. /calculator?category=stone, /calculator?type=villa)
            var seo = await _serviceManager.SEOService.GetSEOByUrlAsync(fullPathWithQuery);

            // 2. If not found and query string exists, try matching by path URL (e.g. /calculator)
            if (seo == null && !string.IsNullOrEmpty(query))
            {
                seo = await _serviceManager.SEOService.GetSEOByUrlAsync(path);
            }

            // 3. Calculator Dynamic Cases via SEO Module PageName
            if (seo == null && path.Contains("/Calculator", StringComparison.OrdinalIgnoreCase))
            {
                var catParam = HttpContext.Request.Query["category"].ToString().ToLowerInvariant();
                var typeParam = HttpContext.Request.Query["type"].ToString().ToLowerInvariant();
                var tabParam = HttpContext.Request.Query["tab"].ToString().ToLowerInvariant();

                string? pageNameKey = null;
                if (!string.IsNullOrEmpty(catParam)) pageNameKey = $"calculator-{catParam}";
                else if (!string.IsNullOrEmpty(typeParam)) pageNameKey = $"calculator-{typeParam}";
                else if (!string.IsNullOrEmpty(tabParam)) pageNameKey = $"calculator-{tabParam}";
                else pageNameKey = "calculator";

                if (!string.IsNullOrEmpty(pageNameKey))
                {
                    seo = await _serviceManager.SEOService.GetSEOByPageNameAsync(pageNameKey);
                }

                if (seo == null)
                {
                    seo = await _serviceManager.SEOService.GetSEOByPageNameAsync("calculator");
                }
            }

            // 4. Dynamic Generation for Product Details Page
            if (seo == null && path.Contains("/Product/Index/", StringComparison.OrdinalIgnoreCase))
            {
                var segments = path.Split('/');
                if (segments.Length > 0 && int.TryParse(segments[segments.Length - 1], out int productId))
                {
                    var product = await _serviceManager.ProductService.GetProductByIdAsync(productId);
                    if (product != null)
                    {
                        var brandText = !string.IsNullOrEmpty(product.Brand) ? $" {product.Brand}" : "";
                        var originText = !string.IsNullOrEmpty(product.CountryOfOrigin) ? $" منشأ {product.CountryOfOrigin}" : "";
                        
                        seo = new Bolcko.Domain.Entities.SEO.DTOs.SEOMetadataDto
                        {
                            PageName = $"Product-{product.Id}",
                            PageTitle = isArabic 
                                ? $"شراء {product.Name}{brandText}{originText} | أسعار التوريد الأردن بلوكو BLOCKO"
                                : $"Buy {product.Name}{brandText} | Best Building Materials Jordan",
                            MetaDescription = isArabic
                                ? $"احصل على {product.Name}{brandText}{originText} بأفضل سعر للبيع والتوريد للمشاريع الإنشائية في الأردن. مواصفات قياسية، جودة معتمدة، وتوصيل فوري للموقع من بلوكو BLOCKO."
                                : $"Get standard {product.Name}{brandText} online. Direct wholesale construction supply & delivery to your jobsite in Jordan. Enquire for bulk pricing today.",
                            MetaKeywords = isArabic
                                ? $"شراء {product.Name}، {product.Brand}، مواد بناء الأردن، توريد مشاريع، أسعار مواد البناء، بلوكو، blocko"
                                : $"buy {product.Name}, {product.Brand}, building materials jordan, blocko construction supplies",
                            PageUrl = $"/Shop/Product/Index/{product.Id}"
                        };
                    }
                }
            }

            // 5. Fallback attempt with page name mapping from database
            if (seo == null)
            {
                var pageName = "Home";
                if (path.Contains("/Calculator", StringComparison.OrdinalIgnoreCase)) pageName = "calculator";
                else if (path.Contains("/Product", StringComparison.OrdinalIgnoreCase)) pageName = "Products";
                else if (path.Contains("/Category", StringComparison.OrdinalIgnoreCase)) pageName = "Categories";
                else if (path.Contains("/Contact", StringComparison.OrdinalIgnoreCase)) pageName = "Contact";
                else if (path.Contains("/About", StringComparison.OrdinalIgnoreCase)) pageName = "About";
                else if (path.Contains("/Quote", StringComparison.OrdinalIgnoreCase)) pageName = "Quote";
                else if (path.Contains("/dataroom", StringComparison.OrdinalIgnoreCase)) pageName = "dataroom";

                seo = await _serviceManager.SEOService.GetSEOByPageNameAsync(pageName);
            }

            // In English mode, ensure SEO title and meta aren't returned in Arabic
            if (!isArabic && seo != null)
            {
                bool hasArabic = (seo.PageTitle?.Any(c => c >= 0x0600 && c <= 0x06FF) ?? false);
                if (hasArabic)
                {
                    var lowerPath = path.ToLowerInvariant();
                    if (lowerPath.Contains("/contact") || string.Equals(seo.PageName, "Contact", StringComparison.OrdinalIgnoreCase))
                    {
                        seo.PageTitle = !string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "Contact Us | BLOCKO Construction Supplies Jordan";
                        seo.MetaDescription = "Get in touch with BLOCKO for wholesale building materials, RFQs, and project supply in Jordan.";
                        seo.MetaKeywords = "contact blocko, building materials amman jordan, wholesale construction supply";
                    }
                    else if (lowerPath.Contains("/about") || string.Equals(seo.PageName, "About", StringComparison.OrdinalIgnoreCase))
                    {
                        seo.PageTitle = !string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "About Us | BLOCKO Building Materials Jordan";
                        seo.MetaDescription = "Learn more about BLOCKO, Jordan's leading digital platform for building materials and project supply.";
                        seo.MetaKeywords = "about blocko, construction supplies jordan, building material supplier amman";
                    }
                    else if (lowerPath.Contains("/calculator") || string.Equals(seo.PageName, "calculator", StringComparison.OrdinalIgnoreCase))
                    {
                        seo.PageTitle = !string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "Construction Quantity Calculator | BLOCKO";
                        seo.MetaDescription = "Calculate concrete, blocks, steel rebar, and mortar quantities accurately for your construction project in Jordan.";
                        seo.MetaKeywords = "building calculator jordan, concrete calculator, rebar quantity calculator";
                    }
                    else if (lowerPath.Contains("/terms"))
                    {
                        seo.PageTitle = !string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "Terms of Service | BLOCKO";
                        seo.MetaDescription = "Terms and conditions of BLOCKO construction materials supply platform.";
                    }
                    else if (lowerPath.Contains("/privacy"))
                    {
                        seo.PageTitle = !string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "Privacy Policy | BLOCKO";
                        seo.MetaDescription = "Privacy policy and data protection terms of BLOCKO.";
                    }
                    else if (lowerPath.Contains("/support"))
                    {
                        seo.PageTitle = !string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "Support & Help Center | BLOCKO";
                        seo.MetaDescription = "Technical and consultative support for contractors and engineers in Jordan.";
                    }
                    else
                    {
                        seo.PageTitle = !string.IsNullOrEmpty(defaultTitle) ? defaultTitle : "BLOCKO - Construction Supplies & Building Materials Jordan";
                        seo.MetaDescription = "Jordan's leading digital platform for building materials, construction supplies, and instant delivery.";
                        seo.MetaKeywords = "building materials jordan, construction supplies amman, wholesale steel cement concrete";
                    }
                }
            }

            // Construct definitive Canonical URL (Resolves GSC Duplicate without user-selected canonical)
            var canonicalBase = "https://www.block-o.com";
            string canonicalPath;
            if (!string.IsNullOrEmpty(seo?.PageUrl))
            {
                canonicalPath = seo.PageUrl.StartsWith("/") ? seo.PageUrl : $"/{seo.PageUrl}";
            }
            else
            {
                canonicalPath = path == "/" ? "" : path;
            }
            var canonicalUrl = $"{canonicalBase}{canonicalPath}";
            ViewBag.CanonicalUrl = canonicalUrl;

            ViewBag.DefaultTitle = defaultTitle ?? (isArabic ? "بلوكو لتوريد مواد البناء | BLOCKO" : "BLOCKO - Building Materials");
            return View(seo);
        }
    }
}
