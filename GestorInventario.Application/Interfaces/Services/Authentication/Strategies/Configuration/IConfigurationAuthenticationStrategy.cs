using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Strategies.Configuration
{
    public interface IConfigurationAuthenticationStrategy
    {
       void  Configure(IServiceCollection services, IConfiguration configuration);
    }
}
