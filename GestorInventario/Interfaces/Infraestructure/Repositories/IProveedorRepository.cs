using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Proveedor;

namespace GestorInventario.Interfaces.Infraestructure.Repositories
{
    public interface IProveedorRepository
    {
        //Consultas
        IQueryable<Proveedore> ObtenerProveedores();
        Task<Proveedore> ObtenerProveedorId(int id);
        //Operaciones
        Task<OperationResult<string>> CrearProveedor(ProveedorViewModel model); 
        Task<OperationResult<string>> EliminarProveedor(int Id);
        Task<OperationResult<string>> EditarProveedor(ProveedorViewModel model, int Id);
       
    }
}
