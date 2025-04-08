namespace GestorInventario.Interfaces.Application
{
    public interface IAuthenticationStrategy
    {
        IServiceCollection ConfigureAuthentication(WebApplicationBuilder builder, IConfiguration configuration);
    }
}
