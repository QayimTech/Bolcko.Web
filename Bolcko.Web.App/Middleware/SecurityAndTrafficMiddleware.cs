using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Blocko.Services.Interfaces.Analytics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bolcko.Web.App.Middleware
{
    public class SecurityAndTrafficMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly string[] SuspiciousPathPatterns = new[]
        {
            @"\.env", @"wp-admin", @"wp-login", @"etc/passwd", @"\.git", @"phpinfo",
            @"setup\.php", @"eval\(", @"base64_", @"bin/bash", @"cmd\.exe", @"\.config$"
        };

        public SecurityAndTrafficMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
            {
                ipAddress = forwardedFor.FirstOrDefault()?.Split(',')[0].Trim() ?? ipAddress;
            }

            var path = context.Request.Path.Value ?? "";
            var method = context.Request.Method;

            // Resolve scope-based security audit service
            var securityService = context.RequestServices.GetRequiredService<ISecurityAuditService>();

            // 1. IP Blacklist Check (Bypass localhost and loopback for safety)
            bool isLocalHost = ipAddress == "127.0.0.1" || ipAddress == "::1" || ipAddress == "localhost";
            if (!isLocalHost && await securityService.IsIpBlockedAsync(ipAddress))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(@"
                    <div style='font-family:sans-serif; text-align:center; padding:50px;' dir='rtl'>
                        <h1 style='color:#e11d48;'>تم حظر الوصول 🚫</h1>
                        <p style='color:#475569;'>تم تقييد عنوان الـ IP الخاص بك لتجاوز سياسات أمان المنصة. يرجى التواصل مع إدارة الموقع لمزيد من التفاصيل.</p>
                    </div>");
                return;
            }

            // 2. Universal Exemption Guard: Admins & Localhost are NEVER flagged for security threats
            bool isAdminUser = context.User.Identity?.IsAuthenticated == true && (context.User.IsInRole("Admin") || context.User.IsInRole("SuperAdmin"));
            if (isAdminUser || isLocalHost)
            {
                await _next(context);
                return;
            }

            // 3. Suspicious Threat Pattern Detection (External Untrusted Traffic Only)
            string threatType = string.Empty;
            string threatDescription = string.Empty;

            foreach (var pattern in SuspiciousPathPatterns)
            {
                if (Regex.IsMatch(path, pattern, RegexOptions.IgnoreCase))
                {
                    threatType = "PathTraversalOrProbe";
                    threatDescription = $"محاولة مسح مسار مشبوه: {path}";
                    break;
                }
            }

            // Check Query string for SQLi/XSS Probes
            var queryString = context.Request.QueryString.Value ?? "";
            if (string.IsNullOrEmpty(threatType) && (queryString.Contains("SELECT", StringComparison.OrdinalIgnoreCase) || queryString.Contains("<script", StringComparison.OrdinalIgnoreCase) || queryString.Contains("UNION", StringComparison.OrdinalIgnoreCase)))
            {
                threatType = "SqlOrXssProbe";
                threatDescription = $"محاولة حقن ثغرة في الاستعلام: {queryString}";
            }

            if (!string.IsNullOrEmpty(threatType))
            {
                await securityService.LogThreatAsync(
                    ipAddress,
                    path,
                    method,
                    threatType,
                    threatDescription,
                    queryString,
                    context.Request.Headers["User-Agent"].ToString()
                );
            }

            // Measure execution time
            var stopwatch = Stopwatch.StartNew();
            await _next(context);
            stopwatch.Stop();

            // 3. Record Traffic & 404 Security Probes
            var statusCode = context.Response.StatusCode;
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var referrer = context.Request.Headers["Referer"].ToString();

            if (statusCode == 404)
            {
                var lowerPath = path.ToLower();

                // Exclude static assets, media extensions, .well-known system paths, and Admin traffic from threat logging
                bool isMediaOrAssetOrSystem = lowerPath.StartsWith("/css") ||
                                              lowerPath.StartsWith("/js") ||
                                              lowerPath.StartsWith("/images") ||
                                              lowerPath.StartsWith("/products") ||
                                              lowerPath.StartsWith("/lib") ||
                                              lowerPath.StartsWith("/favicon") ||
                                              lowerPath.StartsWith("/home/error") ||
                                              lowerPath.StartsWith("/.well-known") ||
                                              lowerPath.EndsWith(".webp") ||
                                              lowerPath.EndsWith(".png") ||
                                              lowerPath.EndsWith(".jpg") ||
                                              lowerPath.EndsWith(".jpeg") ||
                                              lowerPath.EndsWith(".gif") ||
                                              lowerPath.EndsWith(".svg") ||
                                              lowerPath.EndsWith(".ico") ||
                                              lowerPath.EndsWith(".css") ||
                                              lowerPath.EndsWith(".js") ||
                                              lowerPath.EndsWith(".woff") ||
                                              lowerPath.EndsWith(".woff2") ||
                                              lowerPath.EndsWith(".json");

                if (!isAdminUser && !isLocalHost && !isMediaOrAssetOrSystem)
                {
                    // Only log real external suspicious 404 scans
                    await securityService.LogThreatAsync(
                        ipAddress,
                        path,
                        method,
                        "Bad404Scan",
                        $"محاولة مسح مسار غير موجود: {path}",
                        null,
                        userAgent
                    );
                }
            }

            var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();
            var userId = context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null;
            var executionTime = stopwatch.Elapsed.TotalMilliseconds;

            bool isHealthCheck = path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) || 
                                 path.Equals("/ping", StringComparison.OrdinalIgnoreCase) || 
                                 path.Equals("/healthz", StringComparison.OrdinalIgnoreCase);

            if (!isHealthCheck)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = scopeFactory.CreateScope();
                        var analyticsService = scope.ServiceProvider.GetRequiredService<IAnalyticsService>();
                        await analyticsService.RecordVisitAsync(
                            ipAddress,
                            path,
                            method,
                            userAgent,
                            referrer,
                            userId,
                            statusCode,
                            executionTime
                        );
                    }
                    catch
                    {
                        // Silent fallback for analytics background tasks
                    }
                });
            }
        }
    }
}
