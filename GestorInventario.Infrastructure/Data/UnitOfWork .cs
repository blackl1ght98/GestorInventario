using GestorInventario.Application.Interfaces.Data;
using GestorInventario.Application.Interfaces.Repositories;

namespace GestorInventario.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly GestorInventarioContext _context;
     
        public IUserRepository UserRepository { get; private set; }
        public IRoleRepository RoleRepository { get; private set; }
        public IPaypalRepository PaypalRepository { get; private set; }
        public IShopCartRepository ShopCartRepository { get; private set; }
        public IOrderRepository OrderRepository { get; private set; }
        public IProductRepository ProductRepository { get; private set; }
        public IProviderRepository ProviderRepository { get; private set; }
        private bool _disposed = false;

        public UnitOfWork(GestorInventarioContext context, IUserRepository user, IRoleRepository role, IPaypalRepository paypal,
        IShopCartRepository shopCart, IOrderRepository orderRepository, IProductRepository productRepository, IProviderRepository providerRepository)
        {
            _context = context;
            UserRepository = user;
            RoleRepository = role;
            PaypalRepository = paypal;
            ShopCartRepository = shopCart;
            OrderRepository = orderRepository;
            ProductRepository = productRepository;
            ProviderRepository = providerRepository;
        }

    
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
