using GestorInventario.Application.Interfaces.Repositories;

namespace GestorInventario.Application.Interfaces.Data
{
    public interface IUnitOfWork : IDisposable
    {
        IAdminRepository AdminRepository { get; }
        IRoleRepository RoleRepository { get; }
        IPaypalRepository PaypalRepository { get; }
        IShopCartRepository ShopCartRepository { get; }
        IProviderRepository ProviderRepository { get; }
        public IOrderRepository OrderRepository { get;}
        public IProductRepository ProductRepository { get; }
       
       
    }
}
