using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GestorInventario.MetodosExtension
{
    public static  class DbContextExtensions
    {
        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
            string connectionString;
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["DataBaseConection:DBHost"];
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["DataBaseConection:DBName"];
            var dbUserUsername = Environment.GetEnvironmentVariable("DB_SQLUSER") ?? configuration["DataBaseConection:DBUserName"];
            var dbUserPassword = Environment.GetEnvironmentVariable("DB_SQLUSER_PASSWORD") ?? configuration["DataBaseConection:DBPassword"];
            if (isDocker)
            {
              
                 connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID={dbUserUsername};Password={dbUserPassword};TrustServerCertificate=True";
               
            }
            else
            {
                // Cadena de conexión en duro para entorno local
                 connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID={dbUserUsername};Password={dbUserPassword};TrustServerCertificate=True";
               
            }
            services.AddDbContext<GestorInventarioContext>(options =>
            {
                options.UseSqlServer(connectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });
            return services;
        }
    }


}

