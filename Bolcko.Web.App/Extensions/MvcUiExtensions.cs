using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bolcko.Web.App.Extensions
{
    public static class MvcUiExtensions
    {
        public static IServiceCollection AddBlockoMvcInterface(this IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                options.ModelBinderProviders.Insert(0, new Bolcko.Web.App.Binders.InvariantDecimalModelBinderProvider());
            })
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();
            
            return services;
        }

        public static IApplicationBuilder MapBlockoAppEndpoints(this IEndpointRouteBuilder endpoints)
        {
            // Map SignalR Hubs
            endpoints.MapHub<Bolcko.Web.App.Hubs.NotificationHub>("/notificationHub");

            // Map API Controllers (must be before MVC routes)
            endpoints.MapControllers();

            // Root route: serve the shop home page directly at "/"
            // (a 302 redirect here added a full round-trip before first byte
            //  and made PageSpeed measure /Shop/Home/Index instead of /)
            endpoints.MapControllerRoute(
                name: "root",
                pattern: "",
                defaults: new { area = "Shop", controller = "Home", action = "Index" });

            // Vendor Area Dedicated Routes
            endpoints.MapControllerRoute(
                name: "vendor_join",
                pattern: "Vendor/Join",
                defaults: new { area = "Vendor", controller = "Account", action = "Join" });

            endpoints.MapControllerRoute(
                name: "vendor_register",
                pattern: "Vendor/Register",
                defaults: new { area = "Vendor", controller = "Account", action = "Register" });

            endpoints.MapControllerRoute(
                name: "vendor_confirmation",
                pattern: "Vendor/Confirmation",
                defaults: new { area = "Vendor", controller = "Account", action = "Confirmation" });

            endpoints.MapControllerRoute(
                name: "vendor_login",
                pattern: "Vendor/Login",
                defaults: new { area = "Vendor", controller = "Account", action = "Login" });

            endpoints.MapControllerRoute(
                name: "vendor_dashboard",
                pattern: "Vendor/Dashboard",
                defaults: new { area = "Vendor", controller = "Dashboard", action = "Index" });

            endpoints.MapControllerRoute(
                name: "vendor_root",
                pattern: "Vendor",
                defaults: new { area = "Vendor", controller = "Account", action = "Join" });

            // Areas Support
            endpoints.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            // Default Route
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            return (IApplicationBuilder)endpoints;
        }
    }
}
