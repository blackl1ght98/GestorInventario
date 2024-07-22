namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IUnitOfWork
    {
        IPaypalService PaypalService { get; }
    }
}
