using GestorInventario.Domain.Models;

using GestorInventario.Utilities;
using GestorInventario.ViewModels.Productos;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IProductManagementService
    {
        Task<OperationResult<Producto>> CrearProducto(ProductosViewModel model);
        Task<OperationResult<string>> EditarProducto(ProductosViewModel model, int usuarioId);

        Task<OperationResult<string>> EliminarProducto(int Id);
    }
}
