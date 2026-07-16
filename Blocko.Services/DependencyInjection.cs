using Blocko.Persistence.Common;
using FluentValidation;
using Blocko.Services.Implementations;
using Blocko.Services.Implementations.Category;
using Blocko.Services.Implementations.Images;
using Blocko.Services.Implementations.order;

using Blocko.Services.Implementations.Product;
using Blocko.Services.Implementations.shoppingCart;
using Blocko.Services.Implementations.Tender;
using Blocko.Services.Implementations.user;
using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Image;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Product;
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
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration, string contentRootPath)
        {
            services.Configure<ImageSettings>(configuration.GetSection("ImageSettings"));

            services.AddScoped<IServiceManager, ServiceManager>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMarketPriceService, MarketPriceService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ITenderService, TenderService>();
            services.AddScoped(typeof(IPagedList<>), typeof(PagedList<>));
            services.AddScoped<IImageService, ImageService>(sp =>
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
                    sp.GetRequiredService<Blocko.Services.Interfaces.Image.IImageService>(),
                    contentRootPath));
            services.AddScoped<Blocko.Services.Interfaces.Auth.ITokenService, Blocko.Services.Implementations.Auth.TokenService>();
            services.AddMemoryCache();
            services.AddScoped<ITranslationService, TranslationService>();
            services.AddScoped<Blocko.Services.Interfaces.Delivery.IDeliveryService, Blocko.Services.Implementations.Delivery.DeliveryService>();
            services.AddScoped<Blocko.Services.Interfaces.Delivery.IDeliveryDocumentService, Blocko.Services.Implementations.Delivery.DeliveryDocumentService>();

            // Register Validators
            services.AddValidatorsFromAssembly(typeof(Blocko.Services.Validation.ProductImportDtoValidator).Assembly);

            services.AddHttpClient();

            return services;
        }
    }
}