using GestorInventario.Interfaces.Application;

namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface IRefreshTokenStrategyFactory
    {
        IRefreshTokenStrategy CreateStrategy();
    }
}
