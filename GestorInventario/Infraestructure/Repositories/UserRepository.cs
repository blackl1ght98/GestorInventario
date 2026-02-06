using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GestorInventario.Infraestructure.Repositories
{
    public class UserRepository: IUserRepository
    {
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly GestorInventarioContext _context;
        public UserRepository(IHttpContextAccessor contextAccessor, GestorInventarioContext context)
        {
            _contextAccessor = contextAccessor;
            _context = context;

        }
        public async Task<OperationResult<Usuario>> ObtenerUsuarioPorId(int id)
        {
            var usuario = await _context.Usuarios
                .Include(x => x.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (usuario == null)
            {
                return OperationResult<Usuario>.Fail("Usuario no encontrado");
            }
            else
                return OperationResult<Usuario>.Ok("Usuario obtenido con exito", usuario);
        }
        public IQueryable<Usuario> ObtenerUsuarios()
        {
            return _context.Usuarios
                .Include(u => u.IdRolNavigation)
                .AsQueryable();
        }
        public IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId)
        {
            return _context.Usuarios
                .Where(u => u.IdRol == rolId)
                .Include(u => u.IdRolNavigation)
                .AsQueryable();
        }
       
        public async Task<List<Usuario>> ObtenerUsuariosAsync() => await ObtenerUsuarios().ToListAsync();
        public async Task<(Usuario?, string)> ObtenerUsuarioConPedido(int id)
        {
            var usuario = await _context.Usuarios.Include(p => p.Pedidos).FirstOrDefaultAsync(m => m.Id == id);
            return usuario is null ? (null, "Este usuario no tiene pedidos") : (usuario, "Usuario con pedidos encontrado");
        }

    }
}
