namespace GestorInventario.Application.Services
{
    public interface IUnitOfWork
    {
        IPaypalService PaypalService { get;  }
    }
}
