using GestorInventario.Configuracion.Strategies;
using GestorInventario.Interfaces.Application;
using GestorInventario.Middlewares.Strategis;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static class StrategyAuthenticationFactory
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
