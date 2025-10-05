using GestorInventario.Configuracion.Strategies;
using GestorInventario.Interfaces.Application;
using GestorInventario.Middlewares.Strategis;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static class StrategyFactory
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

        public static IAuthProcessingStrategy CreateAuthProcessingStrategy(string authMode)
        {
            return authMode switch
            {
                "AsymmetricDynamic" => new DynamicAsymmetricAuthStrategy(),
                "AsymmetricFixed" => new FixedAsymmetricAuthStrategy(),
                "Symmetric" => new SymmetricAuthStrategy(),
                _ => throw new ArgumentException("Estrategia de procesamiento de autenticación no válida")
            };
        }
    }
}
