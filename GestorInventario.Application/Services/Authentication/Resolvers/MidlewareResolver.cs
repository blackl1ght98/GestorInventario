using GestorInventario.Application.Services.Authentication.Strategies.Middleware;
using GestorInventario.Interfaces.Application.Services.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Application.Services.Authentication.Resolvers
{
    public class MidlewareResolver
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public MidlewareResolver(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }
        
        public IAuthenticationMiddlewareStrategy ResolveMiddleware()
        {
            var mode = _configuration["AuthMode"] ?? Environment.GetEnvironmentVariable("AUTH_MODE");


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
