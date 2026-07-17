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

// Prevent inotify limit issues on Linux containers (e.g. Render) by enabling polling file watcher
System.Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
System.Environment.SetEnvironmentVariable("DOTNET_HS_POLLING_FILE_WATCHER", "1");

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

    // --- Auto-apply pending EF Core migrations on startup (Code First) ---
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Blocko.Persistence.BlockoDbContext>();
        var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // --- Pre-check: Fix corrupted migration history ---
        // If translation columns don't exist in DB but are marked as applied in history,
        // remove that record to force EF to re-apply the migration and create the columns.
        const string translationMigrationId = "20260717045637_AddEnglishTranslationsColumns";
        try
        {
            var nameEnExists = db.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*)::int FROM information_schema.columns " +
                    "WHERE table_name = 'Products' AND column_name = 'NameEn'")
                .AsEnumerable()
                .First() > 0;

            if (!nameEnExists)
            {
                startupLogger.LogWarning("Translation columns missing from DB. Removing stale migration history entry to force re-apply...");
                db.Database.ExecuteSqlRaw(
                    $"DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{translationMigrationId}'");
            }
        }
        catch
        {
            // __EFMigrationsHistory may not exist yet on first run — safe to ignore
        }

        try
        {
            db.Database.Migrate();
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "42701" || ex.SqlState == "42P07")
        {
            // Some migrations were applied to DB directly without being recorded in __EFMigrationsHistory.
            // Strategy: Check which of our new columns actually exist in the DB.
            // Mark only the HISTORICAL migrations (those already applied) as recorded,
            // then retry Migrate() so truly new migrations still get applied.
            startupLogger.LogWarning("Migration conflict ({SqlState}): {Msg}. Syncing __EFMigrationsHistory...", ex.SqlState, ex.MessageText);

            // Check if the new translation columns already exist in DB
            var translationColumnsExist = db.Database
                .SqlQueryRaw<int>(
                    "SELECT COUNT(*)::int FROM information_schema.columns " +
                    "WHERE table_name = 'Products' AND column_name = 'NameEn'")
                .AsEnumerable()
                .First() > 0;

            var allMigrations = db.Database.GetMigrations().ToList();

            // If translation columns don't exist yet, skip registering that migration
            // so Migrate() will actually apply it and create the columns.
            var migrationsToRegister = translationColumnsExist
                ? allMigrations
                : allMigrations.Where(m => m != translationMigrationId);

            foreach (var migrationId in migrationsToRegister)
            {
                db.Database.ExecuteSqlRaw(
                    $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                    $"VALUES ('{migrationId}', '8.0.11') ON CONFLICT DO NOTHING");
            }

            startupLogger.LogInformation("History synced. Retrying Migrate() for new pending migrations...");
            db.Database.Migrate();
            startupLogger.LogInformation("All migrations applied successfully.");
        }
    }

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
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = ctx =>
        {
            // Set Cache-Control header to enable caching on Edge servers (Cloudflare CDN) and client browsers
            const int durationInSeconds = 60 * 60 * 24 * 365; // Cache for 1 year
            ctx.Context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.CacheControl] = 
                "public, max-age=" + durationInSeconds;
        }
    });

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
