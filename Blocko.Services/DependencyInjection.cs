using Blocko.Services.Implementations;
using Blocko.Services.Implementations.User;
using Blocko.Services.Implementations.Product;
using Blocko.Services.Implementations.Category;
using Blocko.Services.Implementations.Order;
using Blocko.Services.Implementations.Tender;
using Blocko.Services.Interfaces;
using Blocko.Services.Interfaces.User;
using Blocko.Services.Interfaces.Product;
using Blocko.Services.Interfaces.Category;
using Blocko.Services.Interfaces.Order;
using Blocko.Services.Interfaces.Tender;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ITenderService, TenderService>();
            
            return services;
        }
    }
}