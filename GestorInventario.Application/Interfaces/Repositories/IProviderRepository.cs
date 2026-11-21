using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IProviderRepository
    {
        //Consultas
        IQueryable<Proveedores> ObtenerProveedores();
        Task<Proveedores> ObtenerProveedorId(int id);
        Task<Proveedores> ObtenerProveedoresConProductos(int Id);
        Task<Proveedores> ObtenerProveedorNombre(string nombreProveedor);
        //Operaciones
 
    
        Task<OperationResult<Proveedores>> AddAsync(Proveedores proveedore);
        Task<OperationResult<Proveedores>> UpdateAsync(Proveedores proveedore);
        Task<OperationResult<Proveedores>> DeleteAsync(Proveedores proveedore);


    }
}
