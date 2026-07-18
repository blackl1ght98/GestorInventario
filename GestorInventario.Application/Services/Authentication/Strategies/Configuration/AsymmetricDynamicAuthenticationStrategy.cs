using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Application.Services.Authentication.Strategies.Configuration
{
    public class AsymmetricDynamicAuthenticationStrategy : IConfigurationAuthenticationStrategy
    {
       

        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
            // Para claves dinámicas por usuario, no podemos configurar un IssuerSigningKey fijo
            // porque cada usuario tiene su propio par RSA. Por eso AddJwtBearer no se usa aquí:
            // la validación la hace el middleware DynamicAsymmetricAuthStrategy que consulta
            // la clave pública en caché por 'kid'.
            //
            // El esquema Cookie sigue siendo necesario para que [Authorize] sin política JWT
            // siga funcionando en endpoints estándar.

            services.AddBaseCookieAuth(
          securePolicy: CookieSecurePolicy.Always,
          includeAccessDeniedPath: true);


        }
    }
}