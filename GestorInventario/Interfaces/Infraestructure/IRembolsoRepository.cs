using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IRembolsoRepository
    {
        Task<IQueryable<Rembolso>> ObtenerRembolsos();
        Task<OperationResult<string>> EliminarRembolso(int id);
    }
}
