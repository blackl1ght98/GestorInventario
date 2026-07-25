using Microsoft.AspNetCore.Http;

namespace GestorInventario.Interfaces.Application.Services.Authentication.Strategies.Middleware
{
  
    public interface IAuthenticationMiddlewareStrategy
    {
        Task ProcessAuthentication(HttpContext context, Func<Task> next);
    }
}
