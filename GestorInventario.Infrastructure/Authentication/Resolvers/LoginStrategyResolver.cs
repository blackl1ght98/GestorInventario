using GestorInventario.Application.Constants;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Domain.enums.Usuario;
using GestorInventario.Infrastructure.Authentication.Login;
using GestorInventario.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Infrastructure.Authentication.Resolvers
{

    public class LoginStrategyResolver:ILoginStrategyResolver
    {
      
        private readonly IServiceProvider _serviceProvider;
        private readonly EnvironmentConfig _environmentConfig;

        public LoginStrategyResolver(IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _environmentConfig = new EnvironmentConfig(configuration);
        }


        public ILoginStrategy Resolve(string? userRole = null)
        {
            var mode = _environmentConfig.LoginMode;

            return mode switch
            {
                "StandardLogin" => ActivatorUtilities.CreateInstance<StandardLoginStrategy>(_serviceProvider),
                "MfaLogin" => ActivatorUtilities.CreateInstance<MfaLoginStrategy>(_serviceProvider),
                "RoleBasedMfa" => ResolveByRole(userRole),
                _ => throw new InvalidOperationException($"Modo de login no soportado: {mode}")
            };
        }
        private ILoginStrategy ResolveByRole(string? userRole)
        {
            
            bool requiresMfa = string.Equals(userRole, Rol.Administrador.ToString(), StringComparison.OrdinalIgnoreCase);

            return requiresMfa
                ? ActivatorUtilities.CreateInstance<MfaLoginStrategy>(_serviceProvider)
                : ActivatorUtilities.CreateInstance<StandardLoginStrategy>(_serviceProvider);
        }
    }
}
