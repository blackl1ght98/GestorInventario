using GestorInventario.Infrastructure.Authentication.Middleware;
using GestorInventario.Infrastructure.Configuration;
using GestorInventario.Infrastructure.Interfaces.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Authentication.Resolvers
{
    public class MidlewareResolver:IMidlewareResolver
    {
        
        private readonly IServiceProvider _serviceProvider;
        private readonly EnvironmentConfig _environmentConfig;

        public MidlewareResolver(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _environmentConfig = new EnvironmentConfig(configuration);
        }
        
        public IAuthenticationMiddlewareStrategy ResolveMiddleware()
        {
            var mode = _environmentConfig.AuthMode;


            return mode switch
            {
                "Symmetric" => ActivatorUtilities.CreateInstance<SymmetricAuthStrategy>(_serviceProvider),

                "AsymmetricFixed" => ActivatorUtilities.CreateInstance<FixedAsymmetricAuthStrategy>(_serviceProvider),

                "AsymmetricDynamic" => ActivatorUtilities.CreateInstance<DynamicAsymmetricAuthStrategy>(_serviceProvider),

                _ => throw new InvalidOperationException($"Modo de autenticación no soportado: {mode}.")
            };
        }
    }
}
