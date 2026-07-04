using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Middlewares.Strategis;
using GestorInventario.Shared.Auth.Strategies;

namespace GestorInventario.Middlewares
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
            var mode = _configuration["AuthMode"] ?? "AsymmetricDynamic";


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
