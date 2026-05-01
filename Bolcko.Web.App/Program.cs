using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Bolcko.Web.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices();

// Required for Identity UI features like SignInManager
builder.Services.AddDbContext<BlockoDbContext>(options =>
          options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
              b => b.MigrationsAssembly(typeof(BlockoDbContext).Assembly.FullName)));

// ???????: ?????? AddIdentityCore ?? ???? ??? Persistence 
// ????? ?? ????? ??? Cookies ?? ??? Web UI ????? ?? ASP.NET Core
builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<BlockoDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddIdentityApiEndpoints<User>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<BlockoDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "Blocko.Auth";
    options.LoginPath = "/Shop/Account/Login";
    options.AccessDeniedPath = "/Shop/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Seed Identity Data
await app.SeedIdentityDataAsync();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);
app.MapGet("/", context =>
{
    context.Response.Redirect("/Shop/Home");
    return Task.CompletedTask;
});
app.Run();