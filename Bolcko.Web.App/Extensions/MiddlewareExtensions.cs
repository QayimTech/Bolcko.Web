using Microsoft.AspNetCore.Builder;

namespace Bolcko.Web.App.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseBlockoSecurityPipeline(this IApplicationBuilder app)
        {
            // The order here is critical for the application to function correctly
            app.UseRouting();
            
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
    }
}
