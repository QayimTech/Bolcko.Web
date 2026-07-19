using Bolcko.Web.App.Extensions;
using Bolcko.Web.App.Services;
using Hangfire;
using QuestPDF;
using Serilog;

// ============================================================================
// BOLCKO E-COMMERCE PLATFORM
// Main Application Entry Point
// ============================================================================
// Architecture: Clean Architecture with Extension Method Organization
// Principles: SOLID, DRY, Single Responsibility Principle
// ============================================================================

// Configure QuestPDF License (Community Edition)
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Configure File System Watcher for Container Environments
// Resolves inotify limit issues on Linux containers (e.g., Render)
ConfigureFileSystemWatcher();

try
{
    Log.Information("Starting Bolcko.Web application...");

    var builder = WebApplication.CreateBuilder(args);

    // =========================================================================
    // STEP 1: Configure Logging
    // =========================================================================
    builder.ConfigureSerilogLogging();

    // =========================================================================
    // STEP 2: Configure Core Application Services
    // =========================================================================
    const long maxRequestSize = 500 * 1024 * 1024; // 500 MB for bulk imports

    builder.Services.AddCoreServices(
        builder.Configuration, 
        builder.Environment.ContentRootPath);

    builder.Services.AddWebLayerServices(builder.Configuration);
    
    builder.Services.AddConfigurationSettings(builder.Configuration);
    
    builder.Services.ConfigureRequestSizeLimits(maxRequestSize);
    
    builder.ConfigureKestrelLimits(maxRequestSize);
    
    builder.Services.AddSessionServices();

    // Register our LogCleanupService for dependency injection
    builder.Services.AddTransient<LogCleanupService>();

    // =========================================================================
    // STEP 2b: Configure Performance Optimizations (NEW)
    // =========================================================================
    builder.Services.AddAdvancedCompression();
    builder.Services.AddMarkupMinification();
    builder.Services.AddAdvancedResponseCaching();

    // =========================================================================
    // STEP 3: Build Application
    // =========================================================================
    var app = builder.Build();

    // =========================================================================
    // STEP 4: Configure Middleware Pipeline
    // =========================================================================
    // Order is critical - each middleware builds upon the previous
    
    // Performance middleware (MUST be first)
    app.UseAdvancedCompression();
    app.UseAdvancedResponseCaching();
    
    app.UseSwaggerDocumentation();
    app.UseEnvironmentSpecificMiddleware();
    app.UseSecurityMiddleware();
    app.UsePerformanceAndSecurityHeaders();
    app.UseStaticFilesWithCaching();
    app.UseFirstRunSetupMiddleware();
    app.UseSessionMiddleware();
    app.UseLocalizationMiddleware();
    app.UseSecurityPipeline();
    app.UseHangfireDashboardMiddleware();
    
    // HTML Minification (MUST be last in pipeline before endpoints)
    app.UseMarkupMinification();

    // =========================================================================
    // STEP 5: Initialize Database
    // =========================================================================
    await app.InitializeDatabaseAsync();

    // ─── TEMP DIAGNOSTIC: Check variants of product 6963 ────────────────────
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Blocko.Persistence.BlockoDbContext>();
            var variants = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                db.ProductVariants.Where(v => v.ProductId == 6963)
            );
            Serilog.Log.Information("[DIAGNOSTIC] Product 6963 has {Count} variants in DB at startup.", variants.Count);
            foreach (var v in variants)
            {
                Serilog.Log.Information("[DIAGNOSTIC] Variant ID: {Id}, Sku: {Sku}, Size: {Size}, Color: {Color}", v.Id, v.Sku, v.Size, v.Color);
            }
        }
    }
    catch (Exception ex)
    {
        Serilog.Log.Error(ex, "[DIAGNOSTIC] Failed to query product 6963 variants at startup.");
    }

    // Schedule recurring log cleanup job (runs daily at midnight UTC)
    RecurringJob.AddOrUpdate<LogCleanupService>(
        "daily-log-cleanup", 
        service => service.CleanOldLogsAsync(10), 
        Cron.Daily);

    // =========================================================================
    // STEP 6: Map Endpoints
    // =========================================================================
    app.MapApplicationEndpoints();
    app.MapSitemapEndpoint();

    // =========================================================================
    // STEP 7: Run Application
    // =========================================================================
    Log.Information("Bolcko.Web application configured successfully. Starting...");
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

// ============================================================================
// Helper Methods
// ============================================================================

/// <summary>
/// Configures file system watcher for container environments
/// Prevents inotify limit issues on Linux containers
/// </summary>
static void ConfigureFileSystemWatcher()
{
    Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
    Environment.SetEnvironmentVariable("DOTNET_HS_POLLING_FILE_WATCHER", "1");
}
