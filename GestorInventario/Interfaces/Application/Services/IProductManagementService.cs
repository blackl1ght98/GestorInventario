using GestorInventario.Application.DTOS.User;
using GestorInventario.Domain.Models;

using GestorInventario.Utilities;
using GestorInventario.ViewModels.Productos;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IProductManagementService
    {
        Task<OperationResult<Producto>> CrearProducto(ProductoDto model);
        Task<OperationResult<string>> EditarProducto(EditarProductoDto model, int usuarioId);

        Task<OperationResult<string>> EliminarProducto(int Id);
    }
}
