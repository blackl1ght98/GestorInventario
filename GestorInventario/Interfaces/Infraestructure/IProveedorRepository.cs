using GestorInventario.Domain.Models;
using GestorInventario.ViewModels.provider;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IProveedorRepository
    {
        IQueryable<Proveedore> ObtenerProveedores();
        Task<(bool, string)> CrearProveedor(ProveedorViewModel model);
        Task<Proveedore> ObtenerProveedorId(int id);
        Task<(bool, string)> EliminarProveedor(int Id);
        Task<(bool, string)> EditarProveedor(ProveedorViewModel model, int Id);
    }
}
