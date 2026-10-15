using GestorInventario.Application.DTOS.Products;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services.Products
{
    public interface IProductService
    {
        Task<OperationResult<Producto>> CrearProducto(ProductoDto model);
        Task<OperationResult<string>> EditarProducto(EditarProductoDto model, int usuarioId);

        Task<OperationResult<string>> EliminarProducto(int Id);
    }
}
