using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Web.App.Extensions;
using Bolcko.Web.App.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

// Configure QuestPDF license
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Configure Serilog first
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bolcko-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Bolcko.Web application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog(); // Use Serilog for logging

    // --- 1. Services Registration (DI) ---
    builder.Services.AddPersistence(builder.Configuration);
    builder.Services.AddServices(builder.Configuration, builder.Environment.ContentRootPath);
    
    // Configure Data Protection to store keys on the filesystem instead of ephemeral memory,
    // which prevents CryptographicExceptions and request rejection during long file uploads on Render.
    var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
    Directory.CreateDirectory(keysFolder);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
        .SetApplicationName("BolckoApp");

    // Web Specific Services (Clean & Expressive)
    builder.Services.AddBlockoIdentitySecurity(builder.Configuration);
    builder.Services.AddBlockoLocalization();
    // Configure Market Settings
    builder.Services.Configure<Bolcko.Web.App.Models.MarketSettings>(builder.Configuration.GetSection("MarketSettings"));

    // Increase form and request size limits for bulk import
    const long MaxRequestSizeInBytes = 500 * 1024 * 1024; // 500 MB
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = MaxRequestSizeInBytes;
    });
    // Configure Kestrel server limits
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestBodySize = MaxRequestSizeInBytes;
    });

    builder.Services.AddBlockoMvcInterface();
    builder.Services.AddBlockoSwagger();
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, Bolcko.Web.App.Hubs.CustomUserIdProvider>();
    builder.Services.AddScoped<Blocko.Services.Interfaces.Notifications.INotificationService, Bolcko.Web.App.Services.NotificationService>();

    // Add Hangfire Services
    builder.Services.AddBlockoHangfire(builder.Configuration);

    // Add Session Services with secure configuration
    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

    var app = builder.Build();
    app.UseDeveloperExceptionPage();

    app.UseBlockoSwagger();

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

    // First-run setup guard: redirects to /Setup if no Admin exists yet
    app.UseFirstRunSetup();

    // Use Session Middleware (before routing/authorization)
    app.UseSession();

    // Localization should be handled early
    app.UseBlockoRequestLocalization();

    // Core Security Pipeline (Routing -> Auth -> Authorization)
    app.UseBlockoSecurityPipeline();

    // Hangfire Dashboard (must be after auth)
    app.UseBlockoHangfireDashboard();

    // --- Automatic Database Migration ---
    // This will apply any pending migrations to the database automatically when the app starts
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<BlockoDbContext>();
        Log.Information("Applying database migrations...");
        await db.Database.MigrateAsync();

        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"DeliveryJobs\" ADD COLUMN IF NOT EXISTS \"DeliveryToken\" text;");
            Log.Information("DeliveryToken column verified/added successfully via raw SQL.");
        }
        catch (Exception sqlEx)
        {
            Log.Warning(sqlEx, "Failed to apply raw SQL alter table for DeliveryToken column.");
        }

        Log.Information("Database migrations applied successfully.");
    }

    // Seed initial data (only in Development for safety)
    // In Production, the /Setup page handles first-run admin creation

    await app.SeedIdentityDataAsync();
    Log.Information("Development identity data seeded");


    // --- 3. Endpoint Mapping ---
    app.MapBlockoAppEndpoints();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
