using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;

namespace Bolcko.Web.App.Middleware
{
    /// <summary>
    /// Redirects every request to /Setup when no Admin user exists yet.
    /// Once an admin is created this middleware becomes a no-op (single DB check cached per request).
    /// </summary>
    public class FirstRunSetupMiddleware
    {
        private readonly RequestDelegate _next;

        public FirstRunSetupMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<User> userManager)
        {
            var path = context.Request.Path.Value ?? "";

            // Always allow /Setup and static files through
            bool isSetupPath   = path.StartsWith("/Setup",   StringComparison.OrdinalIgnoreCase);
            bool isStaticFile  = path.StartsWith("/lib",     StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/css",     StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/js",      StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/images",  StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase)
                              || path.Contains(".");

            if (isSetupPath || isStaticFile)
            {
                await _next(context);
                return;
            }

            // Check if any admin exists
            var admins = await userManager.GetUsersInRoleAsync("Admin");
            if (admins.Count == 0)
            {
                context.Response.Redirect("/Setup");
                return;
            }

            await _next(context);
        }
    }

    // Extension method for clean registration
    public static class FirstRunSetupMiddlewareExtensions
    {
        public static IApplicationBuilder UseFirstRunSetup(this IApplicationBuilder app)
            => app.UseMiddleware<FirstRunSetupMiddleware>();
    }
}
