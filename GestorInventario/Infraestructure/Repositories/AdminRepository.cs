using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace GestorInventario.Infraestructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
       private readonly GestorInventarioContext _context;

        public AdminRepository(GestorInventarioContext context)
        {
            _context = context;
        }
        public IIncludableQueryable<Usuario, Role> ObtenerUsuarios()
        {
            return _context.Usuarios.Include(x => x.IdRolNavigation);
        }
        public Usuario ObtenerPorId(int id)
        {
            //Alternativa --> _context.Usuarios.ExistUserId(confirmar.UserId) esto con metodo de extension
            return _context.Usuarios.Find(id);
        }
        public IEnumerable<Role> ObtenerRoles()
        {
            return _context.Roles.ToList();
        }
        public async Task<Usuario> ExisteEmail(string email)
        {
            return await _context.Usuarios.EmailExists(email);
        }
        public async Task<Usuario> UsuarioConPedido(int id)
        {
            return await _context.Usuarios.Include(p => p.Pedidos).FirstOrDefaultAsync(m => m.Id == id);



        }
    }
}
