using GestorInventario.Application.Interfaces.Services.Authentication.Strategies.Middleware;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Resolvers;

public interface IMidlewareResolver
{
    IAuthenticationMiddlewareStrategy ResolveMiddleware();
}