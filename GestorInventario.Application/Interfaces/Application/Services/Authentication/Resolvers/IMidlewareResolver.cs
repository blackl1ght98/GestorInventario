using GestorInventario.Application.Interfaces.Application.Services.Authentication.Strategies.Middleware;

namespace GestorInventario.Application.Interfaces.Application.Services.Authentication.Resolvers;

public interface IMidlewareResolver
{
    IAuthenticationMiddlewareStrategy ResolveMiddleware();
}