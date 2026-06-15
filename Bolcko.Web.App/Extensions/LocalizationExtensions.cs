using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Bolcko.Web.App.Extensions
{
    public static class LocalizationExtensions
    {
        public static IServiceCollection AddBlockoLocalization(this IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            return services;
        }

        public static IApplicationBuilder UseBlockoRequestLocalization(this IApplicationBuilder app)
        {
            var supportedCultures = new[] { "ar", "en" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            // Cookie provider must be first so it takes priority
            localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());

            return app.UseRequestLocalization(localizationOptions);
        }
    }
}
