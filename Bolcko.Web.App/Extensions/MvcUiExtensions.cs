using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bolcko.Web.App.Extensions
{
    public static class MvcUiExtensions
    {
        public static IServiceCollection AddBlockoMvcInterface(this IServiceCollection services)
        {
            services.AddControllersWithViews()
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
