

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IConfigurationAuthenticationStrategy
    {
       void  Configure(IServiceCollection services, IConfiguration configuration);
    }
}
