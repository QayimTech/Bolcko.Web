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

            // 1. IP Blacklist Check
            if (await securityService.IsIpBlockedAsync(ipAddress))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(@"
                    <div style='font-family:sans-serif; text-align:center; padding:50px; dir:rtl;'>
                        <h1 style='color:#e11d48;'>تم حظر الوصول 🚫</h1>
                        <p style='color:#475569;'>تم تقييد عنوان الـ IP الخاص بك لتجاوز سياسات أمان المنصة. يرجى التواصل مع إدارة الموقع لمزيد من التفاصيل.</p>
                    </div>");
                return;
            }

            // 2. Suspicious Threat Pattern Detection
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

            // 3. Record Traffic & 404 Probes Background Logging
            var statusCode = context.Response.StatusCode;

            if (statusCode == 404 && !path.StartsWith("/css") && !path.StartsWith("/js") && !path.StartsWith("/images"))
            {
                await securityService.LogThreatAsync(
                    ipAddress,
                    path,
                    method,
                    "Bad404Scan",
                    $"طلب صفحة غير موجودة 404: {path}",
                    null,
                    context.Request.Headers["User-Agent"].ToString()
                );
            }

            var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var referrer = context.Request.Headers["Referer"].ToString();
            var userId = context.User.Identity?.IsAuthenticated == true ? context.User.Identity.Name : null;
            var executionTime = stopwatch.Elapsed.TotalMilliseconds;

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
