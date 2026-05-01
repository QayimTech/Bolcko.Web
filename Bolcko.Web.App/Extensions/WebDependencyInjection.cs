using System.Globalization;
using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Blocko.Persistence;

namespace Bolcko.Web.App.Extensions
{
    public static class WebDependencyInjection
    {
        public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Identity & Auth (Must be configured before calling UserManager in Seeder)
            services.AddIdentityCore<User>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<int>>()
            .AddEntityFrameworkStores<BlockoDbContext>()
            .AddDefaultTokenProviders();

            // Required for SignInManager and other Identity UI features
            services.AddIdentityApiEndpoints<User>()
                .AddRoles<IdentityRole<int>>()
                .AddEntityFrameworkStores<BlockoDbContext>();

            // 2. Localization
            services.AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();

            // 3. Cookies configuration
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Blocko.Auth";
                options.LoginPath = "/Shop/Account/Login";
                options.AccessDeniedPath = "/Shop/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);

                options.Events.OnRedirectToLogin = context =>
                {
                    var requestPath = context.Request.Path.Value ?? "";
                    if (requestPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Redirect("/Admin/Account/Login" + "?ReturnUrl=" + System.Net.WebUtility.UrlEncode(requestPath));
                    }
                    else if (requestPath.StartsWith("/Dashboard", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Redirect("/Dashboard/Account/Login" + "?ReturnUrl=" + System.Net.WebUtility.UrlEncode(requestPath));
                    }
                    else
                    {
                        context.Response.Redirect("/Shop/Account/Login" + "?ReturnUrl=" + System.Net.WebUtility.UrlEncode(requestPath));
                    }
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    var requestPath = context.Request.Path.Value ?? "";
                    if (requestPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Redirect("/Admin/Account/Login");
                    }
                    else if (requestPath.StartsWith("/Dashboard", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.Redirect("/Dashboard/Account/Login");
                    }
                    else
                    {
                        context.Response.Redirect("/Shop/Account/AccessDenied");
                    }
                    return Task.CompletedTask;
                };
            });

            return services;
        }

        public static IApplicationBuilder UseWebLocalization(this IApplicationBuilder app)
        {
            var supportedCultures = new[] { "ar", "en" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(supportedCultures[0])
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            return app.UseRequestLocalization(localizationOptions);
        }
    }
}
