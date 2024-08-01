using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProveedorRepository
    {
        IQueryable<Proveedore> ObtenerProveedores();
        Task<(bool, string)> CrearProveedor(ProveedorViewModel model);
        Task<Proveedore> ObtenerProveedorId(int id);
        Task<(bool, string)> EliminarProveedor(int Id);
        Task<(bool, string)> EditarProveedor(ProveedorViewModel model);
    }
}
