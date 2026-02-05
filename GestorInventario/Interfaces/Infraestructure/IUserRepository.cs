using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IUserRepository
    {
       
        Task<OperationResult<Usuario>> ObtenerUsuarioPorId(int id);
    }
}
