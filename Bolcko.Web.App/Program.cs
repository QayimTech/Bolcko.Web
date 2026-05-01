using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Web.App.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 1. Layer Dependencies
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices();

// 2. Web Specific Services (SRP)
builder.Services.AddWebServices(builder.Configuration);

var app = builder.Build();

// 3. Environment & Static Files
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

// 4. Identity Seeding
await app.SeedIdentityDataAsync();

// 5. Auth & Middleware
app.UseAuthentication();
app.UseAuthorization();

// 6. Web Middleware & Routing (SRP)
app.UseWebMiddleware();

app.Run();
