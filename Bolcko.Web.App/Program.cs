using Blocko.Persistence;
using Blocko.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddServices();

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