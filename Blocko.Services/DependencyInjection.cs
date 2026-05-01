using Microsoft.Extensions.DependencyInjection;

namespace Blocko.Services
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            // Add services here
            // services.AddScoped<IProductService, ProductService>();
            
            return services;
        }
    }
}