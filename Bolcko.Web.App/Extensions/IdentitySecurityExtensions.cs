using Bolcko.Domain.Entities.User;
using Blocko.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Bolcko.Web.App.Extensions
{
    public static class IdentitySecurityExtensions
    {
        public static IServiceCollection AddBlockoIdentitySecurity(this IServiceCollection services)
        {
            services.AddIdentity<User, IdentityRole<int>>(options =>
            {
                // Strong password policies for production
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequiredUniqueChars = 6;
                
                // Lockout policies for security
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<BlockoDbContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Blocko.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                
                options.LoginPath = "/Shop/Account/Login";
                options.AccessDeniedPath = "/Shop/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;

                options.Events.OnRedirectToLogin = context =>
                {
                    var requestPath = context.Request.Path.Value ?? "";
                    if (requestPath.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
                        context.Response.Redirect("/Admin/Account/Login?ReturnUrl=" + System.Net.WebUtility.UrlEncode(requestPath));
                    else
                        context.Response.Redirect("/Shop/Account/Login?ReturnUrl=" + System.Net.WebUtility.UrlEncode(requestPath));
                    return Task.CompletedTask;
                };
            });

            return services;
        }
    }
}
