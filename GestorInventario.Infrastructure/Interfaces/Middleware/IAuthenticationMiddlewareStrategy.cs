using Microsoft.AspNetCore.Http;

namespace GestorInventario.Infrastructure.Interfaces.Middleware
{
  
    public interface IAuthenticationMiddlewareStrategy
    {
        Task ProcessAuthentication(HttpContext context, Func<Task> next);
    }
}
