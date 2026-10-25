
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;

namespace GestorInventario.Application.Interfaces.Infrastructure.Common
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
