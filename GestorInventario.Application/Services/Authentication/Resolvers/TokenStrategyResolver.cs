using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Application.Services.Authentication.Resolvers
{
   
    public class TokenStrategyResolver
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public TokenStrategyResolver(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

       
        public IRefreshTokenStrategy ResolveRefreshToken()
        {
            var mode = _configuration["AuthMode"] ?? "AsymmetricDynamic";
          

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
            var mode = _configuration["AuthMode"] ?? "AsymmetricDynamic";
    

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
