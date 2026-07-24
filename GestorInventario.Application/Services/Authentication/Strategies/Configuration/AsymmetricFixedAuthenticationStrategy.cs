using GestorInventario.Interfaces.Application.Services.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Application.Services.Authentication.Strategies.Configuration
{
    public class AsymmetricFixedAuthenticationStrategy : IConfigurationAuthenticationStrategy
    {
       

        public void Configure(IServiceCollection services, IConfiguration configuration)
        {


            services.AddBaseCookieAuth(
         securePolicy: CookieSecurePolicy.Always,
         includeAccessDeniedPath: false);

        }
    }
}