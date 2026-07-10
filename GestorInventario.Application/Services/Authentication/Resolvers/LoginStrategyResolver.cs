using GestorInventario.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Application.Services.Authentication.Resolvers
{
    /// <summary>
    /// Resuelve la <see cref="ILoginStrategy"/> concreta a partir de la clave
    /// de configuración <c>LoginMode</c> ("StandardLogin" | "MfaLogin").
    /// </summary>
    /// <remarks>
    /// Punto único de creación de estrategias de login. Centraliza aquí el switch
    /// evita tener una factoría inline en <c>Composition/DependencyInjectionExtensions</c>
    /// duplicando dependencias y, sobre todo, evita repetir el switch si en el futuro
    /// otra interfaz (<c>ILoginValidator</c>, <c>ILoginAudit</c>…) necesita también
    /// elegir estrategia por configuración: se añade un <c>ResolveXxx()</c> nuevo
    /// aquí y Composition sigue sin tocarse.
    ///
    /// <para>
    /// Las estrategias no reciben <see cref="IConfiguration"/> por constructor, así
    /// que <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider)"/>
    /// resuelve todas sus dependencias desde el contenedor sin argumentos manuales.
    /// Si una nueva estrategia necesitara un valor no registrado (config, primitivo…),
    /// se pasaría como argumento extra a <c>CreateInstance</c>.
    /// </para>
    ///
    /// <para>
    /// El consumidor (p. ej. <c>LoginGenerator</c>) inyecta este resolver, no
    /// <see cref="ILoginStrategy"/> directa: si la inyectara, tendría que conocer
    /// el modo activo y se acoplaría al switch.
    /// </para>
    /// </remarks>
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
