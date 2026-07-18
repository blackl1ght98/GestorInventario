using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace GestorInventario.Application.Services.Authentication.Strategies.Configuration
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