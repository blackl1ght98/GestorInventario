using GestorInventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Configuration
{
    public static  class DbContextExtensions
    {
        public static IServiceCollection AddDatabaseContext(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Usamos nuestro gestor centralizado de configuración
            var envConfig = new EnvironmentConfig(configuration);

            var connectionString = $"Data Source={envConfig.DbHost};Initial Catalog={envConfig.DbName};" +
                                   $"User ID={envConfig.DbUserName};Password={envConfig.DbPassword};" +
                                   $"TrustServerCertificate=True";

            services.AddDbContext<GestorInventarioContext>(options =>
            {
                options.UseSqlServer(connectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            return services;
        }
    }


}

