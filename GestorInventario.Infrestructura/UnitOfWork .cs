using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;


namespace GestorInventario.Infrestructure
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
