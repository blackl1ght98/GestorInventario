
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;

namespace GestorInventario.Application.Interfaces.Infrastructure.Common
{
    public interface IUnitOfWork : IDisposable
    {
        IAdminRepository AdminRepository { get; }
        IUserRepository UserRepository { get; }
        IPaypalRepository PaypalRepository { get; }
        ICarritoRepository CarritoRepository { get; }
        public IPedidoRepository PedidoRepository { get;}
        public IProductoRepository ProductoRepository { get; }
       
       
    }
}
