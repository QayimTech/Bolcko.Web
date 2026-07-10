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
            string[] roles = { "Admin", "DashboardUser", "Customer", "DeliveryDriver", "DeliveryCompanyUser" };
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
                    MustChangePassword = true, // Force password change on first login
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(newAdmin, "BolckoAdmin@2026!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }

            // 3. Seed Market Prices
            var dbContext = scope.ServiceProvider.GetRequiredService<Blocko.Persistence.BlockoDbContext>();
            if (!dbContext.MarketPrices.Any())
            {
                dbContext.MarketPrices.AddRange(
                    new Bolcko.Domain.Entities.Catalog.MarketPrice 
                    { 
                        MaterialName = "حديد تسليح سابك", 
                        Price = 610.50m, 
                        Currency = "د.أ", 
                        UnitOfMeasure = "طن", 
                        LastUpdated = DateTime.UtcNow 
                    },
                    new Bolcko.Domain.Entities.Catalog.MarketPrice 
                    { 
                        MaterialName = "أسمنت الراجحي", 
                        Price = 75.00m, 
                        Currency = "د.أ", 
                        UnitOfMeasure = "طن", 
                        LastUpdated = DateTime.UtcNow 
                    },
                    new Bolcko.Domain.Entities.Catalog.MarketPrice 
                    { 
                        MaterialName = "خرسانة جاهزة", 
                        Price = 45.00m, 
                        Currency = "د.أ", 
                        UnitOfMeasure = "متر مكعب", 
                        LastUpdated = DateTime.UtcNow 
                    }
                );
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
