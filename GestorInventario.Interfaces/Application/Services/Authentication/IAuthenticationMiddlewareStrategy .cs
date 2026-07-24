using Microsoft.AspNetCore.Http;

namespace GestorInventario.Interfaces.Application.Services.Authentication
{
  
    public interface IAuthenticationMiddlewareStrategy
    {
        Task ProcessAuthentication(HttpContext context, Func<Task> next);
    }
}
