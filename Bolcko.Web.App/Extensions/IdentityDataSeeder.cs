using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Bolcko.Web.App.Extensions
{
    public static class IdentityDataSeeder
    {
        public static async Task SeedIdentityDataAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();

            // 1. Seed Roles
            // NOTE:
            // - Admin: full access (Users/SEO/Everything)
            // - DashboardUser: محدود (Products/Orders/Categories)
            // - Customer: متجر
            string[] roles = { "Admin", "DashboardUser", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
                }
            }

            // 2. Seed First Super Admin
            var adminEmail = "admin@blocko.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Super",
                    LastName = "Admin",
                    UserType = UserType.Admin,
                    EmailConfirmed = true,
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(newAdmin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                } 
            }
        }
    }
}
