using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Web.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Services Registration (DI) ---
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices(builder.Configuration);
// Web Specific Services (Clean & Expressive)
builder.Services.AddBlockoIdentitySecurity();
builder.Services.AddBlockoLocalization();
builder.Services.AddBlockoMvcInterface();

// Add Session Services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(7);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// --- 2. Middleware Pipeline (Strict Engineering Order) ---

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

// Use Session Middleware (before routing/authorization)
app.UseSession();

// Localization should be handled early
app.UseBlockoRequestLocalization();

// Core Security Pipeline (Routing -> Auth -> Authorization)
app.UseBlockoSecurityPipeline();

// Seed initial data
await app.SeedIdentityDataAsync();

// --- 3. Endpoint Mapping ---
app.MapBlockoAppEndpoints();

app.Run();
