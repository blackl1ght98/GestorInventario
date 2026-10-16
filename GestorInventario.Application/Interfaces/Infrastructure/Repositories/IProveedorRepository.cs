using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Infrastructure.Repositories
{
    public interface IProveedorRepository
    {
        //Consultas
        IQueryable<Proveedore> ObtenerProveedores();
        Task<Proveedore> ObtenerProveedorId(int id);
        Task<Proveedore> ObtenerProveedoresConProductos(int Id);
        Task<Proveedore> ObtenerProveedorNombre(string nombreProveedor);
        //Operaciones
 
    
        Task<OperationResult<Proveedore>> AddAsync(Proveedore proveedore);
        Task<OperationResult<Proveedore>> UpdateAsync(Proveedore proveedore);
        Task<OperationResult<Proveedore>> DeleteAsync(Proveedore proveedore);


    }
}
