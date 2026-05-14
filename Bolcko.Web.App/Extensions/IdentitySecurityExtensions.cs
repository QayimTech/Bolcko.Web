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
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<BlockoDbContext>()
            .AddDefaultTokenProviders();

            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "Blocko.Auth";
                options.LoginPath = "/Shop/Account/Login";
                options.AccessDeniedPath = "/Shop/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(7);

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
