using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.Login;

namespace GestorInventario.Interfaces.Application.Services.Authentication.Resolvers;

public interface ILoginStrategyResolver
{
    ILoginStrategy Resolve();
}