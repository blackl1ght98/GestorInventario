using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;

namespace GestorInventario.Infraestructure.Utils
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly GestorInventarioContext _context;
     
        public IAdminRepository AdminRepository { get; private set; }
        public IUserRepository UserRepository { get; private set; }
        public IPaypalRepository PaypalRepository { get; private set; }
        public ICarritoRepository CarritoRepository { get; private set; }
        public IPedidoRepository PedidoRepository { get; private set; }
        public IProductoRepository ProductoRepository { get; private set; }
        private bool _disposed = false;

        public UnitOfWork(GestorInventarioContext context, IAdminRepository admin, IUserRepository user, IPaypalRepository paypal,
        ICarritoRepository carrito, IPedidoRepository pedidoRepository, IProductoRepository productoRepository)
        {
            _context = context;
            AdminRepository = admin;
            UserRepository = user;
            PaypalRepository = paypal;
            CarritoRepository = carrito;
            PedidoRepository = pedidoRepository;
            ProductoRepository = productoRepository;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransactionAsync()
        {
            await _context.Database.RollbackTransactionAsync();
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
