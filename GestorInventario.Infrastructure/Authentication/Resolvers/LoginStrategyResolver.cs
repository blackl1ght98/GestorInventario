using GestorInventario.Application.Interfaces.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Application.Interfaces.Services.Authentication.Resolvers;
using GestorInventario.Application.Services.Authentication.Strategies.Login;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Infrastructure.Authentication.Resolvers
{

    public class LoginStrategyResolver:ILoginStrategyResolver
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
            var mode = _configuration["LoginMode"] ?? Environment.GetEnvironmentVariable("LOGIN_MODE");

            return mode switch
            {
                "StandardLogin" => ActivatorUtilities.CreateInstance<StandardLoginStrategy>(_serviceProvider),
                "MfaLogin" => ActivatorUtilities.CreateInstance<MfaLoginStrategy>(_serviceProvider),
                _ => throw new InvalidOperationException($"Modo de login no soportado: {mode}")
            };
        }
    }
}
