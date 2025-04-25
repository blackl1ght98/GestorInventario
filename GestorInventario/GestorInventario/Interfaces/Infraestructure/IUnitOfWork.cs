namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IUnitOfWork
    {
        IPaypalService PaypalService { get; }
       Task<string> CreateProductAndNotifyAsync(string productName, string productDescription, string productType, string productCategory);
    }
}
