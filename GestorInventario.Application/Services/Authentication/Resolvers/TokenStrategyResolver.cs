using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Application.Services.Authentication.Resolvers
{
    /// <summary>
    /// Resuelve las estrategias de emisión de tokens (access y refresh) a partir
    /// de la clave de configuración <c>AuthMode</c>
    /// ("Symmetric" | "AsymmetricFixed" | "AsymmetricDynamic").
    /// </summary>
    /// <remarks>
    /// Punto único de creación de estrategias de token. Centralizar el switch aquí
    /// cumple dos objetivos que justifican la clase:
    /// <list type="number">
    ///   <item>
    ///     Evita una factoría inline en <c>Composition/DependencyInjectionExtensions</c>
    ///     que duplicara dependencias y resultara en registros repetidos propensos
    ///     a errores crípticos del contenedor.
    ///   </item>
    ///   <item>
    ///     Permite resolver DOS interfaces distintas (<see cref="ITokenStrategy"/>
    ///     y <see cref="IRefreshTokenStrategy"/>) desde el mismo modo configurado,
    ///     sin repetir el switch en dos <c>AddScoped</c> separados en Composition.
    ///     Esta es la razón principal por la que existe una clase con dos métodos
    ///     en vez de dos resolvers independientes.
    ///   </item>
    /// </list>
    ///
    /// <para>
    /// <see cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider)"/>
    /// resuelve desde el contenedor todas las dependencias de cada estrategia
    /// (repos, loggers, <c>TokenClaimsBuilder</c>, <c>IUserRepository</c>…).
    /// No se pasan argumentos manuales porque las estrategias no reciben
    /// <see cref="IConfiguration"/> por constructor; si en el futuro alguna lo
    /// necesitara, se añadiría como parámetro extra a <c>CreateInstance</c>.
    /// </para>
    ///
    /// <para>
    /// Los consumidores (<c>TokenGenerator</c>, <c>RefreshTokenGenerator</c>)
    /// inyectan este resolver y nunca <see cref="ITokenStrategy"/> /
    /// <see cref="IRefreshTokenStrategy"/> directas: si las inyectaran, tendrían
    /// que saber qué modo está activo y se acoplaría al switch.
    /// </para>
    /// </remarks>
    public class TokenStrategyResolver
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public TokenStrategyResolver(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Construye la estrategia de emisión de refresh tokens correspondiente
        /// al modo configurado.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Se lanza cuando <c>AuthMode</c> tiene un valor no reconocido.
        /// </exception>
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
