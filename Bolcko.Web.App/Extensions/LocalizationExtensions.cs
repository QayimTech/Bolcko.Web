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
            // Configure Arabic culture number formatting for e-commerce standard (. for decimal, , for thousands)
            var arCulture = new CultureInfo("ar")
            {
                NumberFormat =
                {
                    NumberDecimalSeparator = ".",
                    CurrencyDecimalSeparator = ".",
                    NumberGroupSeparator = ",",
                    CurrencyGroupSeparator = ","
                }
            };

            var enCulture = new CultureInfo("en")
            {
                NumberFormat =
                {
                    NumberDecimalSeparator = ".",
                    CurrencyDecimalSeparator = ".",
                    NumberGroupSeparator = ",",
                    CurrencyGroupSeparator = ","
                }
            };

            var supportedCultures = new[] { arCulture, enCulture };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("ar")
                .AddSupportedCultures("ar", "en")
                .AddSupportedUICultures("ar", "en");

            // Override DefaultRequestCulture with customized CultureInfo instances
            localizationOptions.SupportedCultures = supportedCultures;
            localizationOptions.SupportedUICultures = supportedCultures;

            // Cookie provider must be first so it takes priority
            localizationOptions.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());

            return app.UseRequestLocalization(localizationOptions);
        }
    }
}
