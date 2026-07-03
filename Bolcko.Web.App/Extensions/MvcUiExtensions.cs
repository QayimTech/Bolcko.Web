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

            // Root Redirect
            endpoints.MapGet("/", context =>
            {
                context.Response.Redirect("/Shop/Home/Index");
                return Task.CompletedTask;
            });

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
