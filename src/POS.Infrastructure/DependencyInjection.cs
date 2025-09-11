using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Interfaces;
using POS.Infrastructure.Data.Configurations;

namespace POS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Add PostgreSQL DbContext
            services.AddDbContext<MyAppDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => {
                        b.MigrationsAssembly(typeof(MyAppDbContext).Assembly.FullName);
                        b.MigrationsHistoryTable("__EFMigrationsHistory", "pos");
                    }));

            // Register the DbContext interface for DI
            services.AddScoped<IMyAppDbContext>(provider => provider.GetRequiredService<MyAppDbContext>());


            return services;
        }
    }
}