using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Interfaces.Application.Services.Authentication.Strategies.RefreshToken;

namespace GestorInventario.Interfaces.Application.Services.Authentication.Resolvers;

public interface ITokenStrategyResolver
{
    IRefreshTokenStrategy ResolveRefreshToken();
    ITokenStrategy ResolveToken();

}