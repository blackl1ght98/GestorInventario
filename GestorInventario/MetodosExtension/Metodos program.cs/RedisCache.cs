using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static class RedisCache
    {
        public static IServiceCollection AddRedisCache(this IServiceCollection services,IConfiguration configuration)
        {
            // Guardamos en una variable las cadenas de conexión
            string redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                                            ?? configuration["Redis:ConnectionString"]
                                            ?? configuration["Redis:ConnectionStringLocal"]!;

            // Comprobamos qué cadena de conexión se está usando
            Console.WriteLine($"Attempting to use Redis connection string: {redisConnectionString}");

            try
            {
                // Intenta crear la conexión con Redis
                var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);

                // Llama al servicio de Redis y configura la conexión
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = "SampleInstance";
                });
                services.AddDataProtection().PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect("redis:6379"), "DataProtection-Keys");
                // Al servicio IConnectionMultiplexer se le pasa la conexión creada con Redis
                services.AddSingleton<IConnectionMultiplexer>(provider => connectionMultiplexer);
                // Se muestra la cadena de conexión que se ha usado
                Console.WriteLine($"Using Redis connection string: {redisConnectionString}");
            }
            catch (Exception ex)
            {
                // Si la conexión falla, muestra la cadena de conexión y el error producido
                Console.WriteLine($"Failed to connect using Redis connection string: {redisConnectionString}. Error: {ex.Message}");
                // Usa la cadena de conexión local como alternativa
                string redisConnectionStringLocal = configuration["Redis:ConnectionStringLocal"];
                // Muestra la cadena de conexión local que se va a usar
                Console.WriteLine($"Attempting to use local Redis connection string: {redisConnectionStringLocal}");
                // Intenta crear la conexión con la cadena de conexión local
                var connectionMultiplexerLocal = ConnectionMultiplexer.Connect(redisConnectionStringLocal);
                // Configura el servicio de Redis con la cadena de conexión local
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionStringLocal;
                    options.InstanceName = "SampleInstance";
                });
                // Al servicio IConnectionMultiplexer se le pasa la conexión local creada
                services.AddSingleton<IConnectionMultiplexer>(provider => connectionMultiplexerLocal);
                // Se muestra la cadena de conexión local que se ha usado
                Console.WriteLine($"Using local Redis connection string: {redisConnectionStringLocal}");
            }
            return services;
        }
    }
}
