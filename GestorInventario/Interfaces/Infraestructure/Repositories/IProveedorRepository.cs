using GestorInventario.Application.DTOS.User;
using GestorInventario.Domain.Models;

using GestorInventario.Utilities;
using GestorInventario.ViewModels.Proveedor;

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
