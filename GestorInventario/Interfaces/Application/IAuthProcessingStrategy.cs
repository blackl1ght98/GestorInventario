namespace GestorInventario.Interfaces.Application
{
  
    public interface IAuthProcessingStrategy
    {
      
        Task ProcessAuthentication(HttpContext context, Func<Task> next);
    }
}
