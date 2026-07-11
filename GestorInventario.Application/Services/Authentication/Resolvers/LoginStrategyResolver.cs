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

        /// <summary>
        /// Construye la estrategia de login correspondiente al modo configurado.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Se lanza cuando <c>LoginMode</c> tiene un valor no reconocido, para
        /// fallar pronto en arranque/primera resolución en vez de devolver null.
        /// </exception>
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
