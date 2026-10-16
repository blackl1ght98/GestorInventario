using GestorInventario.Application.Interfaces.Application.Services.Authentication.Strategies.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Authentication.Configuration
{
    public class AsymmetricDynamicAuthenticationStrategy : IConfigurationAuthenticationStrategy
    {
        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
           services.AddBaseCookieAuth(
          securePolicy: CookieSecurePolicy.Always,
          includeAccessDeniedPath: true);
        }
    }
}