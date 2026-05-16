using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.product;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IProductManagementService
    {
        Task<OperationResult<Producto>> CrearProducto(ProductosViewModel model);
        Task<OperationResult<string>> EditarProducto(ProductosViewModel model, int usuarioId);

        Task<OperationResult<string>> EliminarProducto(int Id);
    }
}
