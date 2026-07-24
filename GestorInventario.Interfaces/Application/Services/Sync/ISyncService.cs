using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Sync
{
    public interface ISyncService
    {
        Task<OperationResult<int>> SyncPlansFromPayPalAsync(int pagina);
    }
}
