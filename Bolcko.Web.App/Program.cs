using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Web.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Registration Phase (DI) ---
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices();
builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

// --- 2. Middleware Pipeline ---

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

// 1. Routing MUST be before Auth & Authorization
app.UseRouting();

// 2. Localization can be here
app.UseWebLocalization();

// 3. Auth & Authorization MUST be between Routing and Endpoints
app.UseAuthentication();
app.UseAuthorization();

// 4. Data Seeding
await app.SeedIdentityDataAsync();

// 5. Endpoint Mapping
app.MapGet("/", context =>
{
    context.Response.Redirect("/Shop/Home/Index");
    return Task.CompletedTask;
});

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
