using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Syncs
{
    public interface ISyncService
    {
        Task<OperationResult<int>> SyncPlansFromPayPalAsync(int pagina);
    }
}
