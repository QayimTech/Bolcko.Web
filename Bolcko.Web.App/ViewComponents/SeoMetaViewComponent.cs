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
            var isArabic = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("ar");
            
            // Check direct db match first
            var seo = await _serviceManager.SEOService.GetSEOByPageNameAsync(path);
            
            // Dynamic Auto-Generation for Product Details Page to secure Top-Rank
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
                                : $"buy {product.Name}, {product.Brand}, building materials jordan, blocko construction supplies"
                        };
                    }
                }
            }

            if (seo == null)
            {
                // Fallback attempt with page name mapping
                var pageName = "Home";
                if (path.Contains("/Product", StringComparison.OrdinalIgnoreCase)) pageName = "Products";
                else if (path.Contains("/Category", StringComparison.OrdinalIgnoreCase)) pageName = "Categories";
                else if (path.Contains("/Contact", StringComparison.OrdinalIgnoreCase)) pageName = "Contact";
                else if (path.Contains("/About", StringComparison.OrdinalIgnoreCase)) pageName = "About";
                
                seo = await _serviceManager.SEOService.GetSEOByPageNameAsync(pageName);
            }

            ViewBag.DefaultTitle = defaultTitle ?? (isArabic ? "بلوكو لتوريد مواد البناء | BLOCKO" : "BLOCKO - Building Materials");
            return View(seo);
        }
    }
}
