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

            // Add Hangfire services
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

            // Add the processing server as IHostedService
            services.AddHangfireServer();

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

            // Allow only if authenticated and in Admin role
            return httpContext?.User?.Identity?.IsAuthenticated == true && 
                   httpContext.User.IsInRole("Admin");
        }
    }
}
