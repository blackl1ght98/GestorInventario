using GestorInventario.Application.Interfaces.Services.Authentication.Strategies.Login;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Resolvers;

public interface ILoginStrategyResolver
{
    ILoginStrategy Resolve();
}