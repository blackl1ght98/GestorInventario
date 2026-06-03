namespace GestorInventario.Interfaces.Application
{
    public interface IConfigurationAuthenticationStrategy
    {
        IServiceCollection ConfigureAuthentication(IServiceCollection services, IConfiguration configuration);
    }
}
