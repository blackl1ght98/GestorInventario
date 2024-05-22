using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.MetodosExtension.Tabla_Items_Carrito;
using Microsoft.AspNetCore.Mvc;
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
        public IQueryable<Usuario> ObtenerUsuarios()
        {
            var queryable = from p in _context.Usuarios.Include(x => x.IdRolNavigation)
                            select p;
            return queryable;
        }
        public async Task<Usuario> ObtenerPorId(int id)
        {
            //Alternativa --> _context.Usuarios.ExistUserId(confirmar.UserId) esto con metodo de extension
            return await _context.Usuarios.FindAsync(id);
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
        public async Task<Usuario> Login(string email)
        {
            return await _context.Usuarios.Include(x => x.IdRolNavigation).FirstOrDefaultAsync(u => u.Email == email);
        }
        public async Task<Carrito> ObtenerCarrito(int userId)
        {
            return await _context.Carritos.FindByUserId(userId);
        }
        public async Task<List<ItemsDelCarrito>> ObtenerItemsCarrito(int userIdcarrito)
        {
            return await _context.ItemsDelCarritos
                        .Include(i => i.Producto)

                         .Include(i => i.Producto.IdProveedorNavigation)

                        .Where(i => i.CarritoId == userIdcarrito)
                        .ToListAsync();
        }
        public async Task<List<ItemsDelCarrito>> ConvertirItemsAPedido(int userIdcarrito)
        {
            return await _context.ItemsDelCarritos
                        .Where(i => i.CarritoId == userIdcarrito)
                        .ToListAsync();
        }
        public async Task<ItemsDelCarrito> ItemsDelCarrito(int Id)
        {
            return await _context.ItemsDelCarritos.ItemsCarritoIds(Id);
        }
        public async Task<Producto> Decrementar(int? id)
        {
            return await _context.Productos.FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}
