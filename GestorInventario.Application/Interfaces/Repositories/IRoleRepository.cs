using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IRoleRepository
    {
        
        IQueryable<Roles> ObtenerRoles();
      
        Task<List<Roles>> GetAllAsync();

        Task<Roles?> GetRolByNameAsync(string nombre);
        Task<Roles?> GetByIdRolAsync(int idRol);

        string? ObtenerNombreRolPorId(int idRol);
        //Operaciones
        Task<OperationResult<Roles>> AddAsync(Roles rol);
    }
}
