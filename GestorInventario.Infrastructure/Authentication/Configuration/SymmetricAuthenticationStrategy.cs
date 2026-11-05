using GestorInventario.Application.Interfaces.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Authentication.Configuration
{
    public class SymmetricAuthenticationStrategy : IConfigurationAuthenticationStrategy
    {
        

        public void Configure(IServiceCollection services, IConfiguration configuration)
        { 
            services.AddBaseCookieAuth(
          securePolicy: CookieSecurePolicy.Always,
          includeAccessDeniedPath: false);

        }
    }
}