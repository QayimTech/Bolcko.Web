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
            // 1. Explicitly check for Render Environment Variable
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            // 2. If null/empty (local machine), fallback to appsettings.json
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                connectionString = configuration.GetConnectionString("DefaultConnection");
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found. Check appsettings.json or Environment variables.");
            }

            // Apply pooling limits to prevent connection exhaustion on low-tier cloud DB plans (e.g. Render Postgres)
            if (!connectionString.Contains("Pooling=", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
                builder.Pooling = true;
                builder.MaxPoolSize = 15; // Safe maximum pool limit for concurrent scaling
                builder.MinPoolSize = 0;
                builder.ConnectionIdleLifetime = 15; // Recycle connections quickly
                connectionString = builder.ToString();
            }

            services.AddDbContext<BlockoDbContext>(options =>
                options.UseNpgsql(connectionString,
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
