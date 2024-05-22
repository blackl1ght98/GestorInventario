using GestorInventario.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {
        IQueryable<Usuario> ObtenerUsuarios();
        Task<Usuario> ObtenerPorId(int id);
        IEnumerable<Role> ObtenerRoles();
        Task<Usuario> ExisteEmail(string email);
        Task<Usuario> UsuarioConPedido(int id);
        Task<Usuario> Login(string email);
        Task<Carrito> ObtenerCarrito(int userId);
        Task<List<ItemsDelCarrito>> ObtenerItemsCarrito(int userIdcarrito);
        Task<List<ItemsDelCarrito>> ConvertirItemsAPedido(int userIdcarrito);
        Task<ItemsDelCarrito> ItemsDelCarrito(int Id);
        Task<Producto> Decrementar(int? id);
    }
}
