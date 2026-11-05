namespace GestorInventario.Application.Interfaces.Authentication;

public interface ILoginStrategyResolver
{
    ILoginStrategy Resolve();
}