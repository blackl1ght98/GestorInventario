using GestorInventario.Application.DTOS.Supplier;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Provider;

public interface IProviderService
{
    Task<OperationResult<string>> CrearProveedor(CrearProveedorDto model);
    Task<OperationResult<string>> EditarProveedor(EditarProveedorDto model, int Id);
    Task<OperationResult<string>> EliminarProveedor(int Id);
}