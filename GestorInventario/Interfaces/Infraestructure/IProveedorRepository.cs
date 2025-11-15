using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.provider;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProveedorRepository
    {
        IQueryable<Proveedore> ObtenerProveedores();
        Task<OperationResult<string>> CrearProveedor(ProveedorViewModel model);
        Task<OperationResult<Proveedore>> ObtenerProveedorId(int id);
        Task<OperationResult<string>> EliminarProveedor(int Id);
        Task<OperationResult<string>> EditarProveedor(ProveedorViewModel model, int Id);
        Task<List<Usuario>> ObtenerProveedoresLista();
    }
}
