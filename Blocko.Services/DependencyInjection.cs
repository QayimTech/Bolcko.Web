using Blocko.Persistence.Common;
using FluentValidation;
using Blocko.Services.Implementations;
using Blocko.Services.Implementations.Category;
using Blocko.Services.Implementations.Images;
using Blocko.Services.Implementations.order;
using Blocko.Services.Implementations.Product;
using Blocko.Services.Implementations.SEO;
using Blocko.Services.Implementations.shoppingCart;
using Blocko.Services.Implementations.Tender;
using Blocko.Services.Implementations.user;
using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Image;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.SEO;
using Blocko.Services.Interfaces.ShoppingCart;
using Blocko.Services.Interfaces.Tender;
using Blocko.Services.Interfaces.User;
using Bolcko.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Blocko.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(
            this IServiceCollection services,
            IConfiguration configuration,
            string contentRootPath)
        {
            services.Configure<ImageSettings>(configuration.GetSection("ImageSettings"));

            // ─── Delivery & Supplier API HttpClient Registration ─────────────────────
            services.AddHttpClient<Blocko.Services.Interfaces.Delivery.IDeliveryApiService, Blocko.Services.Implementations.Delivery.DeliveryApiService>();
            services.AddHttpClient<Blocko.Services.Interfaces.Supplier.ISupplierApiService, Blocko.Services.Implementations.Supplier.SupplierApiService>();

            // ─── SEO Service (must be registered BEFORE ServiceManager) ───────────
            // Uses ILogger<T> which is always available from ASP.NET Core DI
            services.AddScoped<IProductSeoService>(sp =>
                new ProductSeoService(
                    sp.GetRequiredService<Bolcko.Domain.Interfaces.IUnitOfWork>(),
                    sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ProductSeoService>>()));

            // ─── Service Manager (aggregates all services via Lazy pattern) ───────
            services.AddScoped<IServiceManager>(sp =>
                new ServiceManager(
                    sp.GetRequiredService<Bolcko.Domain.Interfaces.IUnitOfWork>(),
                    sp.GetRequiredService<Blocko.Services.Interfaces.Notifications.INotificationService>(),
                    sp.GetRequiredService<Blocko.Services.Interfaces.Delivery.IDeliveryDocumentService>(),
                    sp.GetRequiredService<IEmailSender>(),
                    sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User>>(),
                    sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILoggerFactory>(),
                    sp.GetRequiredService<IProductSeoService>()));

            // ─── Individual Services ──────────────────────────────────────────────
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMarketPriceService, MarketPriceService>();

            services.AddScoped<IOrderService>(sp =>
                new OrderService(
                    sp.GetRequiredService<Bolcko.Domain.Interfaces.IUnitOfWork>(),
                    sp.GetRequiredService<Blocko.Services.Interfaces.Notifications.INotificationService>(),
                    sp.GetRequiredService<IEmailSender>(),
                    sp.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Bolcko.Domain.Entities.User.User>>()));

            services.AddScoped<ITenderService, TenderService>();
            services.AddScoped(typeof(IPagedList<>), typeof(PagedList<>));

            services.AddScoped<IImageService>(sp =>
                new ImageService(
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ImageSettings>>(),
                    sp.GetRequiredService<System.Net.Http.HttpClient>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ImageService>>(),
                    contentRootPath));

            services.AddScoped<IShoppingCartService, ShoppingCartService>();
            services.AddScoped<IProjectService, ProjectService>();

            services.AddScoped<Bolcko.Domain.Interfaces.IBulkImportService>(sp =>
                new Blocko.Services.Imports.BulkImportService(
                    sp.GetRequiredService<Bolcko.Domain.Interfaces.IUnitOfWork>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Bolcko.Domain.Entities.Product.DTOs.ProductImportDto>>(),
                    sp.GetRequiredService<FluentValidation.IValidator<Bolcko.Domain.Entities.Catalog.DTOs.CategoryImportDto>>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Blocko.Services.Imports.BulkImportService>>(),
                    sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
                    sp.GetRequiredService<IImageService>(),
                    contentRootPath));
            services.AddScoped<Blocko.Services.Imports.BulkImportService>();

            services.AddScoped<Blocko.Services.Interfaces.Auth.ITokenService, Blocko.Services.Implementations.Auth.TokenService>();
            services.AddMemoryCache();
            services.AddScoped<ITranslationService, TranslationService>();
            services.AddScoped<Blocko.Services.Interfaces.Delivery.IDeliveryService, Blocko.Services.Implementations.Delivery.DeliveryService>();
            services.AddScoped<Blocko.Services.Interfaces.Delivery.IDeliveryDocumentService, Blocko.Services.Implementations.Delivery.DeliveryDocumentService>();

            // Register FluentValidation validators
            services.AddValidatorsFromAssembly(typeof(Blocko.Services.Validation.ProductImportDtoValidator).Assembly);

            services.AddHttpClient();

            return services;
        }
    }
}