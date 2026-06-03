using GestorInventario.Interfaces.Application;
using GestorInventario.Middlewares.Strategis;

namespace GestorInventario.Middlewares
{
    public class AuthenticationStrategyFactory
    {
        //Metodo fabrica que recibe el modo de autenticacion
        public static IAuthenticationMiddlewareStrategy Create(string authMode)
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
