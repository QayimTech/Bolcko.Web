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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
// ... ???? ??? Usings

namespace Blocko.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ??? ??? ????????? ?? appsettings.json
            services.Configure<ImageSettings>(configuration.GetSection("ImageSettings"));

            services.AddScoped<IServiceManager, ServiceManager>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IMarketPriceService, MarketPriceService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ITenderService, TenderService>();

            // ??????? ?????? ????
            services.AddScoped<IImageService, ImageService>();

            services.AddScoped<IShoppingCartService, ShoppingCartService>();
            services.AddHttpClient();

            return services;
        }
    }
}