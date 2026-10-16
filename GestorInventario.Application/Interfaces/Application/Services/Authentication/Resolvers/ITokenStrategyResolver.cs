using GestorInventario.Application.Interfaces.Application.Services.Authentication.Strategies.AccessToken;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Strategies.RefreshToken;

namespace GestorInventario.Application.Interfaces.Application.Services.Authentication.Resolvers;

public interface ITokenStrategyResolver
{
    IRefreshTokenStrategy ResolveRefreshToken();
    ITokenStrategy ResolveToken();

}