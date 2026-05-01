using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Web.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Registration Phase (DI) ---
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

// --- 2. Middleware Pipeline (Order is Critical for SRP & Security) ---

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

// Localization should be early
app.UseWebLocalization();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Data Seeding
await app.SeedIdentityDataAsync();

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
