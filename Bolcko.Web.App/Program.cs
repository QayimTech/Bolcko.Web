using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Web.App.Extensions;
using Microsoft.AspNetCore.Identity;
using Bolcko.Domain.Entities.User;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// 1. Core Persistence & Services
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices();

// 2. Identity Configuration (CRITICAL: MUST BE REGISTERED HERE FOR MIDDLEWARE TO SEE IT)
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
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

// 3. Localization & MVC
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// 4. Authentication Cookies & Smart Redirection
builder.Services.ConfigureApplicationCookie(options =>
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
        else if (requestPath.StartsWith("/Dashboard", StringComparison.OrdinalIgnoreCase))
            context.Response.Redirect("/Dashboard/Account/Login?ReturnUrl=" + System.Net.WebUtility.UrlEncode(requestPath));
        else
            context.Response.Redirect("/Shop/Account/Login?ReturnUrl=" + System.Net.WebUtility.UrlEncode(requestPath));
        return Task.CompletedTask;
    };
});

var app = builder.Build();

// --- PIPELINE ORDER IS CRITICAL ---

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// 1. Localization early
var supportedCultures = new[] { "ar", "en" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

// 2. Routing defines the endpoint
app.UseRouting();

// 3. Auth MUST be after Routing and before Authorization/Endpoints
app.UseAuthentication();
app.UseAuthorization();

// 4. Data Seeding
await app.SeedIdentityDataAsync();

// 5. Mapping Endpoints
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/", context =>
{
    context.Response.Redirect("/Shop/Home/Index");
    return Task.CompletedTask;
});

app.Run();
