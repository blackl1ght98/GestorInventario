using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IRefundRepository
    {
        Task<IQueryable<Rembolso>> ObtenerRembolsos();
        Task<OperationResult<string>> EliminarRembolso(int id);
    }
}
