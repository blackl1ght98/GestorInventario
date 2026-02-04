using GestorInventario.Interfaces.Application;
using GestorInventario.Middlewares.Strategis;

namespace GestorInventario.Middlewares
{
    public class AuthenticationProcessingFactory
    {
        //Metodo fabrica que recibe el modo de autenticacion
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
