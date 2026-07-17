using Blocko.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Bolcko.Web.App.Extensions;

/// <summary>
/// Extension methods for database initialization, migrations, and seeding
/// </summary>
public static class DatabaseExtensions
{
    #region Database Initialization

    /// <summary>
    /// Initializes the database with automatic migration and optional seeding
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        await app.ApplyMigrationsAsync();
        await app.SeedDataAsync();
    }

    /// <summary>
    /// Applies pending database migrations automatically on startup
    /// </summary>
    private static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BlockoDbContext>();
        
        Log.Information("Applying database migrations...");
        await db.Database.MigrateAsync();
        
        // Apply additional schema changes that migrations might miss
        await ApplyRawSchemaChangesAsync(db);
        
        Log.Information("Database migrations applied successfully.");
    }

    /// <summary>
    /// Applies raw SQL schema changes for specific database features
    /// </summary>
    private static async Task ApplyRawSchemaChangesAsync(BlockoDbContext db)
    {
        try
        {
            // Add DeliveryToken column if it doesn't exist (PostgreSQL syntax)
            await db.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "DeliveryJobs" ADD COLUMN IF NOT EXISTS "DeliveryToken" text;""");
            
            Log.Information("DeliveryToken column verified/added successfully via raw SQL.");
        }
        catch (Exception sqlEx)
        {
            Log.Warning(sqlEx, "Failed to apply raw SQL alter table for DeliveryToken column.");
        }
    }

    #endregion

    #region Data Seeding

    /// <summary>
    /// Seeds initial data based on environment
    /// </summary>
    private static async Task SeedDataAsync(this WebApplication app)
    {
        // Only seed identity data in Development
        if (app.Environment.IsDevelopment())
        {
            await app.SeedIdentityDataAsync();
            Log.Information("Development identity data seeded");
        }
        else
        {
            // In Production, the /Setup page handles first-run admin creation
            Log.Information("Production mode: skipping automatic data seeding. Use /Setup for initial admin creation.");
        }
    }

    #endregion
}
