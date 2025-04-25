namespace GestorInventario.Interfaces.Application
{
    public interface IAuthProcessingStrategy
    {
        Task ProcessAuthentication(HttpContext context, WebApplicationBuilder builder, Func<Task> next);
    }
}
