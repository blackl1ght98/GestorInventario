using GestorInventario.Configuracion.Strategies;
using GestorInventario.Interfaces.Application;
using GestorInventario.Middlewares.Strategis;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    /// <summary>
    /// Fábrica que crea y configura la estrategia de autenticación (middleware) según el modo elegido (AuthMode).
    /// Se usa principalmente en la configuración de AddAuthentication / AddJwtBearer / AddCookie.
    /// </summary>
    public static class AuthenticationConfigurationStrategyFactory
    {
        public static IAuthenticationStrategy CreateAuthenticationStrategy(string authMode)
        {

            return authMode switch
            {
                "AsymmetricDynamic" => new AsymmetricDynamicAuthenticationStrategy(),
                "AsymmetricFixed" => new AsymmetricFixedAuthenticationStrategy(),
                "Symmetric" => new SymmetricAuthenticationStrategy(),
                _ => throw new ArgumentException("Estrategia de autenticación no válida")
            };
        }

       
    }
}
