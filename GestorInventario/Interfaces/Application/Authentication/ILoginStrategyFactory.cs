namespace GestorInventario.Interfaces.Application.Authentication
{
    public interface ILoginStrategyFactory
    {
        ILoginStrategy GetStrategy();
    }
}
