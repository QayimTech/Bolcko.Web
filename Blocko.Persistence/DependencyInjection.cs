using Blocko.Persistence.Repositories;
using Blocko.Persistence.Repositories.Product;
using Blocko.Persistence.Repositories.Category;
using Blocko.Persistence.Repositories.Order;
using Blocko.Persistence.Repositories.Tender;
using Blocko.Persistence.Repositories.Project;
using Blocko.Persistence.Repositories.MarketPrice;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Bolcko.Domain.Entities.User;
using Bolcko.Domain.Interfaces;
using Blocko.Persistence.Repositories.user;
using Bolcko.Domain.Common;
using Blocko.Persistence.Common;

namespace Blocko.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<BlockoDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(BlockoDbContext).Assembly.FullName)));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ITenderRepository, TenderRepository>();
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IMarketPriceRepository, MarketPriceRepository>();
            services.AddScoped(typeof(IPagedList<>), typeof(PagedList<>));
            return services;
        }
    }
}
