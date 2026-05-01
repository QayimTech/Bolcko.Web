using Blocko.Services.Implementations;
using Blocko.Services.Interfaces;
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