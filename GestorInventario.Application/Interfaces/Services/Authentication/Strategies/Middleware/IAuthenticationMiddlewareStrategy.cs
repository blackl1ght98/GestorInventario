using Microsoft.AspNetCore.Http;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Strategies.Middleware
{
  
    public interface IAuthenticationMiddlewareStrategy
    {
        Task ProcessAuthentication(HttpContext context, Func<Task> next);
    }
}
