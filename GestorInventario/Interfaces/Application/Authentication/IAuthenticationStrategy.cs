namespace GestorInventario.Interfaces.Application
{
    public interface IAuthenticationStrategy
    {
        IServiceCollection ConfigureAuthentication(IServiceCollection services, IConfiguration configuration);
    }
}
