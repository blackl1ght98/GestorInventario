using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IRembolsoRepository
    {
        Task<IQueryable<Rembolso>> ObtenerRembolsos();
        Task<(bool, string)> EliminarRembolso(int id);
    }
}
