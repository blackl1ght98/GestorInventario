namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IUnitOfWork : IDisposable
    {
        IPaypalRepository PaypalRepository { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
