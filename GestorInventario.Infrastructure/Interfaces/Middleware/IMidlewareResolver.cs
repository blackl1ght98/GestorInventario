namespace GestorInventario.Infrastructure.Interfaces.Middleware;

public interface IMidlewareResolver
{
    IAuthenticationMiddlewareStrategy ResolveMiddleware();
}