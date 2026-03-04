using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class UserRepository: IUserRepository
    {
       
        private readonly GestorInventarioContext _context;
        public UserRepository( GestorInventarioContext context)
        {
         
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
        public async Task ConfirmEmail(ConfirmRegistrationDto confirm)
        {

            var usuarioUpdate = _context.Usuarios.AsTracking().FirstOrDefault(x => x.Id == confirm.UserId);
            if (usuarioUpdate != null)
            {
                usuarioUpdate.ConfirmacionEmail = true;
                await _context.UpdateEntityAsync(usuarioUpdate);
            }
        }

    }
}
