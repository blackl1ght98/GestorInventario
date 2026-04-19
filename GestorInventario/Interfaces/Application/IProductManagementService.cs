using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.product;

namespace GestorInventario.Interfaces.Application
{
    public interface IProductManagementService
    {
        Task<OperationResult<Producto>> CrearProducto(ProductosViewModel model);
        Task<OperationResult<string>> EditarProducto(ProductosViewModel model, int usuarioId);
        Task<OperationResult<string>> AgregarProductoAlCarrito(int idProducto, int cantidad, int usuarioId);
        Task<OperationResult<string>> EliminarProducto(int Id);
    }
}
