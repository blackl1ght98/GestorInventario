using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Application.Interfaces.Authentication
{
    public interface IConfigurationAuthenticationStrategy
    {
       void  Configure(IServiceCollection services, IConfiguration configuration);
    }
}
