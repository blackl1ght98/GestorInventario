using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IUserRepository
    {
       
        Task<OperationResult<Usuario>> ObtenerUsuarioPorId(int id);
        Task<List<Usuario>> ObtenerUsuariosAsync();
        IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId);
        Task<(Usuario?, string)> ObtenerUsuarioConPedido(int id);
        IQueryable<Usuario> ObtenerUsuarios();
    }
}
