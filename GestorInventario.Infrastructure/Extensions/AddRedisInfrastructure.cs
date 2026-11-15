using GestorInventario.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;


namespace GestorInventario.Infrastructure.Extensions
{
    public static class RedisServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureRedis(
            this IServiceCollection services,
            IConfiguration configuration,
            ILogger logger)
        {
            var envConfig = new EnvironmentConfig(configuration);
            string connectionString = envConfig.RedisConectionString;

            try
            {
                var multiplexer = ConnectionMultiplexer.Connect(connectionString);

                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = connectionString;
                    options.InstanceName = "GestorInventario";
                });

            

                services.AddSingleton<IConnectionMultiplexer>(multiplexer);

                logger.LogInformation("Redis conectado en {Connection}", connectionString);
                return services;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fallo conectando a Redis en {Connection}", connectionString);
                throw;
            }
        }

        
    }
}