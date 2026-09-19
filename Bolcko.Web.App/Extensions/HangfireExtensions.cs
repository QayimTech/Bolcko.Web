using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bolcko.Web.App.Extensions
{
    public static class HangfireExtensions
    {
        public static IServiceCollection AddBlockoHangfire(this IServiceCollection services, IConfiguration configuration)
        {
            // Read connection string
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found for Hangfire.");
            }

            // Ensure IPv4 is used instead of IPv6 [::1] on Windows to avoid SocketException in VS Debugger
            if (connectionString.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = connectionString.Replace("Host=localhost", "Host=127.0.0.1", StringComparison.OrdinalIgnoreCase);
            }

            // Add Hangfire services
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString), new PostgreSqlStorageOptions
                {
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    InvisibilityTimeout = TimeSpan.FromMinutes(5),
                    DistributedLockTimeout = TimeSpan.FromMinutes(10),
                    PrepareSchemaIfNecessary = true
                }));

            // Add the processing server as IHostedService
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = Math.Min(Environment.ProcessorCount * 2, 8);
            });

            return services;
        }

        public static IApplicationBuilder UseBlockoHangfireDashboard(this IApplicationBuilder app)
        {
            // Allow only Admins to access the Hangfire Dashboard
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = new[] { new HangfireAuthorizationFilter() }
            });

            return app;
        }
    }

    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            if (httpContext == null) return false;

            // Allow bypass in local development for easier debugging
            var env = httpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
            if (env != null && env.EnvironmentName == "Development")
            {
                return true;
            }

            // Allow only if authenticated and in Admin or SuperAdmin role
            return httpContext.User?.Identity?.IsAuthenticated == true && 
                   (httpContext.User.IsInRole("Admin") || httpContext.User.IsInRole("SuperAdmin"));
        }
    }
}
