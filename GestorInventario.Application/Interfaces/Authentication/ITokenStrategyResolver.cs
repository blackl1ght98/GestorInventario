namespace GestorInventario.Application.Interfaces.Authentication;

public interface ITokenStrategyResolver
{
    IRefreshTokenStrategy ResolveRefreshToken();
    ITokenStrategy ResolveToken();

}