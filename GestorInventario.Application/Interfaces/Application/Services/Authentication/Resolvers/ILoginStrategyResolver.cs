using GestorInventario.Application.Interfaces.Application.Services.Authentication.Strategies.Login;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Resolvers;

public interface ILoginStrategyResolver
{
    ILoginStrategy Resolve();
}