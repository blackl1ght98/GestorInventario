using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface ISyncService
    {
        Task<OperationResult<int>> SyncPlansFromPayPalAsync(int pagina);
    }
}
