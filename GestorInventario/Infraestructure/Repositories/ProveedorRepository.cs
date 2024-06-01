using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly ILogger<ProveedorRepository> _logger;
        public ProveedorRepository(GestorInventarioContext context, ILogger<ProveedorRepository> logger)
        {
            _context = context;
            _logger = logger;
        }
        public IQueryable<Proveedore> ObtenerProveedores()
        {
            var proveedores = from p in _context.Proveedores
                              select p;
            return proveedores;
        }
        public async Task<(bool, string)> CrearProveedor(ProveedorViewModel model)
        {
            var proveedor = new Proveedore()
            {
                NombreProveedor = model.NombreProveedor,
                Contacto = model.Contacto,
                Direccion = model.Direccion,
            };
            _context.AddEntity(proveedor);
            return (true, null);
        }
        public async Task<Proveedore> ObtenerProveedorId(int id)
        {
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);
            return proveedor;
        }
        public async Task<(bool, string)> EliminarProveedor(int Id)
        {
            var proveedor = await _context.Proveedores.Include(p => p.Productos).FirstOrDefaultAsync(m => m.Id == Id);
            if (proveedor == null)
            {
                return (false, "Proveedor no encontrado");
            }
            if (proveedor.Productos.Any())
            {
                return (false, "El proveedor no se puede eliminar porque tiene productos asociados");
            }
            _context.DeleteEntity(proveedor);
            return (true, null);
        }
        public async Task<(bool, string)> EditarProveedor(ProveedorViewModel model)
        {
            try
            {
                var proveedor = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (proveedor == null)
                {
                    return (false, "El proveedor no existe");
                }
                proveedor.NombreProveedor = model.NombreProveedor;
                proveedor.Contacto = model.Contacto;
                proveedor.Direccion = model.Direccion;
                _context.UpdateEntity(proveedor);
                return (true, null);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError("Error de concurrencia", ex);
                var proveedor = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == model.Id);
                if (proveedor == null)
                {
                    return (false, "El proveedor no existe");
                }
                _context.Entry(proveedor).Reload();
                _context.Entry(proveedor).State = EntityState.Modified;
                proveedor.NombreProveedor = model.NombreProveedor;
                proveedor.Contacto = model.Contacto;
                proveedor.Direccion = model.Direccion;
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError("Ocurrio una excepcion no esperada", ex);
                return (false, null);
            }
           
        }
    }
}
