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
    }
}
