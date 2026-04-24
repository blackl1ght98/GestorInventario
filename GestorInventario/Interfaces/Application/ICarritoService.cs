using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application
{
    public interface ICarritoService
    {
        Task EliminarCarritoActivoAsync();
        Task<OperationResult<Pedido>> CrearCarritoUsuario(int userId);
        Task<OperationResult<string>> Incremento(int id);
        Task<OperationResult<string>> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId);
        Task<OperationResult<string>> Decremento(int id);
        Task<OperationResult<string>> EliminarProductoCarrito(int id);
    }
}
