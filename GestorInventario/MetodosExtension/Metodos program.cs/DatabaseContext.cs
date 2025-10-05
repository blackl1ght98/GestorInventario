using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static  class DatabaseContext
    {
        public static IServiceCollection AddDatabaseContext(this IServiceCollection services, IConfiguration configuration)
        {
            var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
            var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? configuration["DataBaseConection:DBHost"];
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? configuration["DataBaseConection:DBName"];
            var dbUserName = Environment.GetEnvironmentVariable("DB_USERNAME") ?? configuration["DataBaseConection:DBUserName"];
            var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD") ?? configuration["DataBaseConection:DBPassword"];

            string connectionString = isDocker ? $"Data Source={dbHost};Initial Catalog={dbName};User ID=sa;Password={dbPassword};TrustServerCertificate=True"
                                             : $"Data Source={dbHost};Initial Catalog={dbName};User ID={dbUserName};Password={dbPassword};TrustServerCertificate=True";
            if (dbUserName == null || dbUserName == "" || dbPassword == null || dbPassword == "")
            {
                connectionString = $"Data Source={dbHost};Initial Catalog={dbName};Integrated Security=True;TrustServerCertificate=True";


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
