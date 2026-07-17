using Bolcko.Web.App.Middleware;
using Microsoft.Net.Http.Headers;

namespace Bolcko.Web.App.Extensions;

/// <summary>
/// Extension methods for configuring WebApplication middleware pipeline
/// Follows Pipeline Pattern - each method adds specific middleware in correct order
/// </summary>
public static class WebApplicationExtensions
{
    #region Environment-specific Middleware

    /// <summary>
    /// Configures exception handling based on environment
    /// Development: Detailed exception page
    /// Production: Generic error handler + HSTS
    /// </summary>
    public static void UseEnvironmentSpecificMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }
    }

    #endregion

    #region Security & HTTPS

    /// <summary>
    /// Configures HTTPS redirection and security headers
    /// </summary>
    public static void UseSecurityMiddleware(this WebApplication app)
    {
        app.UseHttpsRedirection();
    }

    #endregion

    #region Static Files

    /// <summary>
    /// Configures static files with long-term caching for optimal performance
    /// </summary>
    public static void UseStaticFilesWithCaching(this WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                // Set Cache-Control header to enable caching on Edge servers (Cloudflare CDN) and client browsers
                const int durationInSeconds = 60 * 60 * 24 * 365; // Cache for 1 year
                ctx.Context.Response.Headers[HeaderNames.CacheControl] = 
                    "public, max-age=" + durationInSeconds + ", immutable";
                
                // Add Vary header for better CDN caching
                ctx.Context.Response.Headers[HeaderNames.Vary] = "Accept-Encoding";
            }
        });
    }

    #endregion

    #region Custom Middleware

    /// <summary>
    /// Configures first-run setup middleware (redirects to setup if no admin exists)
    /// </summary>
    public static void UseFirstRunSetupMiddleware(this WebApplication app)
    {
        app.UseFirstRunSetup();
    }

    #endregion

    #region Session & Localization

    /// <summary>
    /// Configures session middleware (must be before routing)
    /// </summary>
    public static void UseSessionMiddleware(this WebApplication app)
    {
        app.UseSession();
    }

    /// <summary>
    /// Configures request localization
    /// </summary>
    public static void UseLocalizationMiddleware(this WebApplication app)
    {
        app.UseBlockoRequestLocalization();
    }

    #endregion

    #region Routing & Security Pipeline

    /// <summary>
    /// Configures core security pipeline (Routing -> Authentication -> Authorization)
    /// </summary>
    public static void UseSecurityPipeline(this WebApplication app)
    {
        app.UseBlockoSecurityPipeline();
    }

    #endregion

    #region Additional Services

    /// <summary>
    /// Configures Swagger/OpenAPI documentation
    /// </summary>
    public static void UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseBlockoSwagger();
    }

    /// <summary>
    /// Configures Hangfire dashboard for background jobs
    /// </summary>
    public static void UseHangfireDashboardMiddleware(this WebApplication app)
    {
        app.UseBlockoHangfireDashboard();
    }

    #endregion

    #region Endpoints

    /// <summary>
    /// Maps all application endpoints
    /// </summary>
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        app.MapBlockoAppEndpoints();
    }

    /// <summary>
    /// Maps sitemap endpoint separately
    /// </summary>
    public static void MapSitemapEndpoint(this WebApplication app)
    {
        app.MapControllerRoute(
            name: "sitemap",
            pattern: "sitemap.xml",
            defaults: new { controller = "SiteMap", action = "Index" });
    }

    #endregion
}
