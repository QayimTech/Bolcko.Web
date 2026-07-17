using Blocko.Persistence;
using Blocko.Services;
using Bolcko.Web.App.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Serilog;
using Serilog.Events;

namespace Bolcko.Web.App.Extensions;

/// <summary>
/// Extension methods for configuring WebApplicationBuilder services
/// Follows Single Responsibility Principle - each method configures one aspect
/// </summary>
public static class WebApplicationBuilderExtensions
{
    #region Logging Configuration

    /// <summary>
    /// Configures Serilog logging with structured output to console and file
    /// </summary>
    public static void ConfigureSerilogLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/bolcko-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();
    }

    #endregion

    #region Core Services Configuration

    /// <summary>
    /// Registers core application services: Persistence, Business Services, and Data Protection
    /// </summary>
    public static void AddCoreServices(this IServiceCollection services, 
        IConfiguration configuration, 
        string contentRootPath)
    {
        // Persistence Layer
        services.AddPersistence(configuration);
        
        // Business Services Layer
        services.AddServices(configuration, contentRootPath);
        
        // Data Protection - essential for preventing CryptographicExceptions
        ConfigureDataProtection(services, contentRootPath);
    }

    private static void ConfigureDataProtection(IServiceCollection services, string contentRootPath)
    {
        var keysFolder = Path.Combine(contentRootPath, "App_Data", "Keys");
        Directory.CreateDirectory(keysFolder);

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
            .SetApplicationName("BolckoApp");
    }

    #endregion

    #region Web Layer Services

    /// <summary>
    /// Configures web-specific services: MVC, Identity, Localization, Swagger, SignalR
    /// </summary>
    public static void AddWebLayerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Identity and Security
        services.AddBlockoIdentitySecurity(configuration);
        
        // Localization and Globalization
        services.AddBlockoLocalization();
        
        // MVC and UI
        services.AddBlockoMvcInterface();
        
        // API Documentation
        services.AddBlockoSwagger();
        
        // Real-time Communication
        services.AddSignalR();
        services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, Hubs.CustomUserIdProvider>();
        
        // Notification Service
        services.AddScoped<Blocko.Services.Interfaces.Notifications.INotificationService, 
            Services.NotificationService>();
        
        // Background Job Processing
        services.AddBlockoHangfire(configuration);
    }

    #endregion

    #region Configuration Settings

    /// <summary>
    /// Binds configuration sections to strongly-typed settings objects
    /// </summary>
    public static void AddConfigurationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MarketSettings>(configuration.GetSection("MarketSettings"));
    }

    #endregion

    #region Request Size Limits

    /// <summary>
    /// Configures request size limits for handling large file uploads
    /// </summary>
    public static void ConfigureRequestSizeLimits(this IServiceCollection services, long maxRequestSizeInBytes)
    {
        // Form options for multipart requests
        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = maxRequestSizeInBytes;
        });
    }

    /// <summary>
    /// Configures Kestrel server limits
    /// </summary>
    public static void ConfigureKestrelLimits(this WebApplicationBuilder builder, long maxRequestBodySize)
    {
        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Limits.MaxRequestBodySize = maxRequestBodySize;
        });
    }

    #endregion

    #region Session Configuration

    /// <summary>
    /// Configures distributed memory cache and session state
    /// </summary>
    public static void AddSessionServices(this IServiceCollection services)
    {
        services.AddDistributedMemoryCache();
        
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
    }

    #endregion
}
