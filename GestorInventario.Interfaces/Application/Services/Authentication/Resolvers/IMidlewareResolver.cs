using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.Middleware;

namespace GestorInventario.Interfaces.Application.Services.Authentication.Resolvers;

public interface IMidlewareResolver
{
    IAuthenticationMiddlewareStrategy ResolveMiddleware();
}