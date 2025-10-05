using GestorInventario.Application.Services;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Application.Services.Authentication.Token_generation;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static class TokenServices
    {
        public static IServiceCollection AddTokenServices(this IServiceCollection services, bool useRedis) 
        {
            services.AddTransient<ITokenStrategyFactory, TokenStrategyFactory>(provider => {

                //Se resuelve las dependencias necesarias para crear una instancia de "TokenGenerator", esto asegura que se configure correctamente
                var redis = provider.GetRequiredService<IDistributedCache>();
                var memoryCache = provider.GetRequiredService<IMemoryCache>();
                var configuration = provider.GetRequiredService<IConfiguration>();
                var context = provider.GetRequiredService<GestorInventarioContext>();
                var logger = provider.GetRequiredService<ILogger<TokenGenerator>>();

                // Inicialmente se establece en null ya que el valor se asignará si se está usando Redis
                IConnectionMultiplexer connectionMultiplexer = null;
                // Si se está usando Redis...
                if (useRedis)
                {
                    // Se obtiene la conexión de Redis del proveedor de servicios
                    connectionMultiplexer = provider.GetService<IConnectionMultiplexer>();
                }
                // Devuelve una nueva instancia de TokenGenerator con sus dependencia
                return new TokenStrategyFactory(configuration, context, redis, memoryCache, connectionMultiplexer, logger);
            });

            services.AddTransient<IRefreshTokenMethod, RefreshTokenMethod>(provider =>
            {
                var redis = provider.GetRequiredService<IDistributedCache>();
                var memoryCache = provider.GetRequiredService<IMemoryCache>();
                var configuration = provider.GetRequiredService<IConfiguration>();
                var context = provider.GetRequiredService<GestorInventarioContext>();
                var logger = provider.GetRequiredService<ILogger<RefreshTokenMethod>>();
                // Inicialmente se establece en null ya que el valor se asignará si se está usando Redis
                IConnectionMultiplexer connectionMultiplexer = null;
                // Si se está usando Redis...
                if (useRedis)
                {
                    // Se obtiene la conexión de Redis del proveedor de servicios
                    connectionMultiplexer = provider.GetService<IConnectionMultiplexer>();
                }
                return new RefreshTokenMethod(context, configuration, memoryCache, redis, connectionMultiplexer, logger);
            });
            return services;
        }
    }
}
