using GestorInventario.Application.Interfaces.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Application.Interfaces.Services.Authentication.Strategies.RefreshToken;

namespace GestorInventario.Application.Interfaces.Services.Authentication.Resolvers;

public interface ITokenStrategyResolver
{
    IRefreshTokenStrategy ResolveRefreshToken();
    ITokenStrategy ResolveToken();

}