using GestorInventario.Application.DTOS.Supplier;
using GestorInventario.Application.Interfaces.Application.Services.Provider;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Application.Services.Provider;

public class ProviderService : IProviderService
{
    private readonly IProveedorRepository _repository;

    public ProviderService(IProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task<OperationResult<string>> CrearProveedor(CrearProveedorDto model)
    {
        var existingProvider = await _repository.ObtenerProveedorNombre(model.NombreProveedor);
        if (existingProvider != null)
        {
            return OperationResult<string>.Fail("Ya existe un proveedor registrado con ese nombre");
        }

        var proveedor = new Proveedore()
        {
            NombreProveedor = model.NombreProveedor,
            Contacto = model.Contacto,
            Direccion = model.Direccion,
            IdUsuario = model.IdUsuario
        };
        await _repository.AddAsync(proveedor);

        return OperationResult<string>.Ok("Proveedor creado con exito");
    }

    public async Task<OperationResult<string>> EditarProveedor(EditarProveedorDto model, int Id)
    {
        var proveedor = await _repository.ObtenerProveedorId(Id);
        if (proveedor == null)

        {
            return OperationResult<string>.Fail();
        }

        await ActualizarProveedor(proveedor, model);
        return OperationResult<string>.Ok("Proveedor editado con éxito");
    }

    private async Task ActualizarProveedor(Proveedore proveedor, EditarProveedorDto model)
    {
        proveedor.NombreProveedor = model.NombreProveedor;
        proveedor.Contacto = model.Contacto;
        proveedor.Direccion = model.Direccion;
        proveedor.IdUsuario = model.IdUsuario;
        await _repository.UpdateAsync(proveedor);
    }

    public async Task<OperationResult<string>> EliminarProveedor(int Id)
    {
        var proveedor = await _repository.ObtenerProveedoresConProductos(Id);
        if (proveedor == null)
        {
            return OperationResult<string>.Fail();
        }

        if (proveedor.Productos.Any())
        {
            return OperationResult<string>.Fail("El proveedor no se puede eliminar porque tiene productos asociados");
        }

        await _repository.DeleteAsync(proveedor);

        return OperationResult<string>.Ok("Proveedor eliminado con exito");
    }
}