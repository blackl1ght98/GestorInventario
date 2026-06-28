using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services
{
    public interface IProductManagementService
    {
        Task<OperationResult<Producto>> CrearProducto(ProductoDto model);
        Task<OperationResult<string>> EditarProducto(EditarProductoDto model, int usuarioId);

        Task<OperationResult<string>> EliminarProducto(int Id);
    }
}
