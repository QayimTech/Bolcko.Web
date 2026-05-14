using Blocko.Services.Implementations;
using Blocko.Services.Implementations.Product;
using Blocko.Services.Implementations.Category;
using Blocko.Services.Implementations.Tender;
using Blocko.Services.Implementations.Image;
using Blocko.Services.Implementations.ShoppingCart;
using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.User;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Tender;
using Blocko.Services.Interfaces.Image;
using Blocko.Services.Interfaces.ShoppingCart;
using Microsoft.Extensions.DependencyInjection;
using Blocko.Services.Implementations.user;
using Blocko.Services.Implementations.order;

namespace Blocko.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IServiceManager, ServiceManager>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMarketPriceService, MarketPriceService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ITenderService, TenderService>();
            services.AddScoped<IImageService, ImageService>();
            services.AddScoped<IShoppingCartService, ShoppingCartService>();
            services.AddHttpClient();

            return services;
        }
    }
}