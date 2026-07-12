using GestorInventario.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Application.Services.Authentication.Resolvers
{
    
    public class LoginStrategyResolver
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public LoginStrategyResolver(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

       
        public ILoginStrategy Resolve()
        {
            var mode = _configuration["LoginMode"] ?? "MfaLogin";

            return mode switch
            {
                "StandardLogin" => ActivatorUtilities.CreateInstance<StandardLoginStrategy>(_serviceProvider),
                "MfaLogin" => ActivatorUtilities.CreateInstance<MfaLoginStrategy>(_serviceProvider),
                _ => throw new InvalidOperationException($"Modo de login no soportado: {mode}")
            };
        }
    }
}
