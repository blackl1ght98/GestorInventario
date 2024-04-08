using GestorInventario.Domain.Models;
using Microsoft.EntityFrameworkCore.Query;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {
        IIncludableQueryable<Usuario, Role> ObtenerUsuarios();
        Usuario ObtenerPorId(int id);
        IEnumerable<Role> ObtenerRoles();
        Task<Usuario> ExisteEmail(string email);
        Usuario UsuarioConPedido(int id);   
    }
}
