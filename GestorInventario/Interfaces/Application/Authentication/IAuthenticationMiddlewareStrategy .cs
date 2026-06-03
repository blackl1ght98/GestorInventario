namespace GestorInventario.Interfaces.Application
{
  
    public interface IAuthenticationMiddlewareStrategy
    {
      
        Task ProcessAuthentication(HttpContext context, Func<Task> next);
    }
}
