using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using WebMarkupMin.AspNetCore8;

namespace Bolcko.Web.App.Extensions;

/// <summary>
/// Performance optimization extensions for response compression, caching, and minification
/// Following Google's PageSpeed Insights best practices
/// </summary>
public static class PerformanceExtensions
{
    #region Response Compression

    /// <summary>
    /// Configures Brotli and Gzip compression for optimal content delivery
    /// </summary>
    public static void AddAdvancedCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            
            // Compress all common content types
            options.MimeTypes = new[]
            {
                "text/plain",
                "text/css",
                "text/javascript",
                "text/html",
                "text/json",
                "application/json",
                "application/javascript",
                "application/xml",
                "text/xml",
                "image/svg+xml",
                "font/woff2",
                "font/woff",
                "font/ttf"
            };
        });

        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });

        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Optimal;
        });
    }

    #endregion

    #region Web Markup Minification

    /// <summary>
    /// Configures HTML minification to reduce response size
    /// </summary>
    public static void AddMarkupMinification(this IServiceCollection services)
    {
        services.AddWebMarkupMin(options =>
        {
            options.AllowMinificationInDevelopmentEnvironment = true;
            options.AllowCompressionInDevelopmentEnvironment = true;
        })
        .AddHtmlMinification(options =>
        {
            options.MinificationSettings.RemoveRedundantAttributes = true;
            options.MinificationSettings.RemoveHttpProtocolFromAttributes = true;
            options.MinificationSettings.RemoveHttpsProtocolFromAttributes = true;
            options.MinificationSettings.RemoveHtmlComments = true;
            options.MinificationSettings.CollapseBooleanAttributes = true;
        });
    }

    #endregion

    #region Response Caching

    /// <summary>
    /// Configures response caching with optimal policies
    /// </summary>
    public static void AddAdvancedResponseCaching(this IServiceCollection services)
    {
        services.AddResponseCaching(options =>
        {
            options.UseCaseSensitivePaths = false;
            options.MaximumBodySize = 1024 * 1024 * 64; // 64 MB
        });
    }

    #endregion

    #region Application Builder Extensions

    /// <summary>
    /// Uses response compression middleware
    /// </summary>
    public static void UseAdvancedCompression(this IApplicationBuilder app)
    {
        app.UseResponseCompression();
    }

    /// <summary>
    /// Uses web markup minification middleware
    /// </summary>
    public static void UseMarkupMinification(this IApplicationBuilder app)
    {
        app.UseWebMarkupMin();
    }

    /// <summary>
    /// Uses response caching middleware
    /// </summary>
    public static void UseAdvancedResponseCaching(this IApplicationBuilder app)
    {
        app.UseResponseCaching();
    }

    /// <summary>
    /// Adds security and performance headers
    /// </summary>
    public static void UsePerformanceAndSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            // Performance headers
            context.Response.Headers.Append("X-DNS-Prefetch-Control", "on");
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=63072000; includeSubDomains; preload");
            
            // Caching headers for static resources
            if (context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js") ||
                context.Request.Path.StartsWithSegments("/lib") ||
                context.Request.Path.StartsWithSegments("/images"))
            {
                context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
            }

            await next();
        });
    }

    #endregion
}
