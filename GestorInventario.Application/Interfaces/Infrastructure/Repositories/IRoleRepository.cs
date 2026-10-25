using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Infrastructure.Repositories
{
    public interface IRoleRepository
    {
        //
        IQueryable<Role> ObtenerRoles();
      
        Task<List<Role>> GetAllAsync();

        Task<Role?> GetRolByNameAsync(string nombre);
        Task<Role?> GetByIdRolAsync(int idRol);

        string? ObtenerNombreRolPorId(int idRol);
        //Operaciones
        Task<OperationResult<Role>> AddAsync(Role rol);
    }
}
