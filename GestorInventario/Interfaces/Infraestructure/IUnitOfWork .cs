using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IUnitOfWork : IDisposable
    {
        IAdminRepository AdminRepository { get; }
        IUserRepository UserRepository { get; }
        IPaypalRepository PaypalRepository { get; }
        ICarritoRepository CarritoRepository { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
       
    }
}
