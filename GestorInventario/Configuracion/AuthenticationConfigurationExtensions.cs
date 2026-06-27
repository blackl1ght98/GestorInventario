using GestorInventario.Configuracion.Strategies;

namespace GestorInventario.Configuracion
{
    public static  class AuthenticationConfigurationExtensions
    {
        public const string AuthModeConfigKey = "AuthMode";
        public static IServiceCollection AddJwtAuthentication(
      this IServiceCollection services,
      IConfiguration configuration)
        {
            var mode = configuration[AuthModeConfigKey] ?? "AsymmetricDynamic";

            // Logger seguro: antes de Build(), usamos LoggerFactory por defecto
            var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
            var logger = loggerFactory.CreateLogger("AddJwtAuthentication");

            switch (mode)
            {
                case "Symmetric":
                    new SymmetricAuthenticationStrategy(logger)
                        .Configure(services, configuration);
                    break;

                case "AsymmetricFixed":
                    new AsymmetricFixedAuthenticationStrategy(logger)
                        .Configure(services, configuration);
                    break;

                case "AsymmetricDynamic":
                    new AsymmetricDynamicAuthenticationStrategy(logger)
                        .Configure(services, configuration);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Modo de autenticación no soportado: {mode}. " +
                        $"Valores válidos: Symmetric, AsymmetricFixed, AsymmetricDynamic.");
            }

            return services;
        }
    }
}
