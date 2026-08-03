
using GestorInventario.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace GestorInventario.Extensions
{
    public static  class DbContextExtensions
    {
        public static IServiceCollection AddDatabaseContext(
     this IServiceCollection services,
     IConfiguration configuration)
        {
            var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";

            string connectionString = isDocker
                ? BuildDockerConnectionString(configuration)
                : BuildLocalConnectionString(configuration);

            services.AddDbContext<GestorInventarioContext>(options =>
            {
                options.UseSqlServer(connectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            });

            return services;
        }

        private static string BuildDockerConnectionString(IConfiguration configuration)
        {
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST")
                ?? configuration["DataBaseConection:DBHost"];
            var dbName = Environment.GetEnvironmentVariable("DB_NAME")
                ?? configuration["DataBaseConection:DBName"];
            var dbUserName = Environment.GetEnvironmentVariable("DB_SQLUSER")
                ?? configuration["DataBaseConection:DBUserName"];
            var dbPassword = Environment.GetEnvironmentVariable("DB_SQLUSER_PASSWORD")
                ?? configuration["DataBaseConection:DBPassword"];

            if (string.IsNullOrWhiteSpace(dbHost)
                || string.IsNullOrWhiteSpace(dbName)
                || string.IsNullOrWhiteSpace(dbUserName)
                || string.IsNullOrWhiteSpace(dbPassword))
            {
                throw new InvalidOperationException(
                    "Faltan variables de entorno para la conexión Docker (DB_HOST, DB_NAME, DB_SQLUSER, DB_SQLUSER_PASSWORD).");
            }

            return $"Data Source={dbHost};Initial Catalog={dbName};" +
                   $"User ID={dbUserName};Password={dbPassword};" +
                   $"TrustServerCertificate=True";
        }

        private static string BuildLocalConnectionString(IConfiguration configuration)
        {
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST")
                ?? configuration["DataBaseConection:DBHost"];
            var dbName = Environment.GetEnvironmentVariable("DB_NAME")
                ?? configuration["DataBaseConection:DBName"];

            if (string.IsNullOrWhiteSpace(dbHost) || string.IsNullOrWhiteSpace(dbName))
            {
                throw new InvalidOperationException(
                    "Faltan variables de configuración local (DB_HOST, DB_NAME).");
            }

            return $"Data Source={dbHost};Initial Catalog={dbName};" +
                   $"Integrated Security=True;TrustServerCertificate=True";
        }
    }


}

