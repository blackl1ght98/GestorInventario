using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Infrastructure.Authentication.AccessToken;
using GestorInventario.Infrastructure.Authentication.RefreshToken;
using GestorInventario.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Authentication.Resolvers
{
    
    
    public class TokenStrategyResolver:ITokenStrategyResolver
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly EnvironmentConfig _environmentConfig;

        public TokenStrategyResolver(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _environmentConfig = new EnvironmentConfig(configuration);
        }

       
        public IRefreshTokenStrategy ResolveRefreshToken()
        {
            var mode = _environmentConfig.AuthMode;
          

            return mode switch
            {
                "Symmetric" => ActivatorUtilities.CreateInstance<RefreshSymmetricToken>(_serviceProvider),

                "AsymmetricFixed" => ActivatorUtilities.CreateInstance<RefreshAsymetricFixedToken>(_serviceProvider),

                "AsymmetricDynamic" => ActivatorUtilities.CreateInstance<RefreshAsymmetricDynamicToken>(_serviceProvider),

                _ => throw new InvalidOperationException($"Modo de autenticación no soportado: {mode}.")
            };
        }

        public ITokenStrategy ResolveToken()
        {
            var mode = _environmentConfig.AuthMode;
    

            return mode switch
            {
                "Symmetric" => ActivatorUtilities.CreateInstance<SymmetricTokenStrategy>(_serviceProvider),

                "AsymmetricFixed" => ActivatorUtilities.CreateInstance<AsymmetricFixedTokenStrategy>(_serviceProvider),

                "AsymmetricDynamic" => ActivatorUtilities.CreateInstance<AsymmetricDynamicTokenStrategy>(_serviceProvider),

                _ => throw new InvalidOperationException($"Modo de autenticación no soportado: {mode}.")
            };
        }
    }
}
