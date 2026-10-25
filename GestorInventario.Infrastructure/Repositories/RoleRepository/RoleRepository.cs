using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Infrastructure.Extensions;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Repositories.RoleRepository
{
    public class RoleRepository: IRoleRepository
    {
       
        private readonly GestorInventarioContext _context;
        
       
        public RoleRepository(GestorInventarioContext context)
        {
         
            _context = context;
            
        }
        public async Task<List<Role>> GetAllAsync() => await _context.Roles.ToListAsync();
        public string? ObtenerNombreRolPorId(int idRol)
        {
            var roles = ObtenerRoles();
            return roles.FirstOrDefault(r => r.Id == idRol)?.Nombre;
        }
        public  IQueryable<Role> ObtenerRoles()
        {
            var roles = _context.Roles.Include(x => x.Usuarios).AsQueryable();
            return roles;
        }
        public async Task<Role?> GetRolByNameAsync(string nombre)
            => await _context.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre);

        public async Task<Role?> GetByIdRolAsync(int idRol)
            => await _context.Roles.FirstOrDefaultAsync(r => r.Id == idRol);
      
        public async Task<OperationResult<Role>> AddAsync(Role rol)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(rol);
                return OperationResult<Role>.Ok(rol);
            });
        }
        
      
       
    }
}
