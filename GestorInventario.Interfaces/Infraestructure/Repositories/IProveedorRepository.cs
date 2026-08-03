using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Supplier;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Infraestructure.Repositories
{
    public interface IProveedorRepository
    {
        //Consultas
        IQueryable<Proveedore> ObtenerProveedores();
        Task<Proveedore> ObtenerProveedorId(int id);
        //Operaciones
        Task<OperationResult<string>> CrearProveedor(CrearProveedorDto model); 
        Task<OperationResult<string>> EliminarProveedor(int Id);
        Task<OperationResult<string>> EditarProveedor(EditarProveedorDto model, int Id);
       
    }
}
