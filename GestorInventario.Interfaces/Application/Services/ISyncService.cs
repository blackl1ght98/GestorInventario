
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface ISyncService
    {
        Task<OperationResult<int>> SyncPlansFromPayPalAsync(int pagina);
    }
}
