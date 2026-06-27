namespace GestorInventario.Interfaces.Application
{
    public interface IConfigurationAuthenticationStrategy
    {
       void  Configure(IServiceCollection services, IConfiguration configuration);
    }
}
