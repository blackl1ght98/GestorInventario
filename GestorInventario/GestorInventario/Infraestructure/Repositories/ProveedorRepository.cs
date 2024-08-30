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
        public IQueryable<Proveedore> ObtenerProveedores()=>from p in _context.Proveedores select p;             
        public async Task<(bool, string)> CrearProveedor(ProveedorViewModel model)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var proveedor = new Proveedore()
                {
                    NombreProveedor = model.NombreProveedor,
                    Contacto = model.Contacto,
                    Direccion = model.Direccion,
                };
                await _context.AddEntityAsync(proveedor);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError("Error al crear el proveedor",ex);
                await transaction.RollbackAsync();
                return (false, "Error al crear el proveedor");
            }
           
        }
        public async Task<Proveedore> ObtenerProveedorId(int id)=>await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);
          
        
        public async Task<(bool, string)> EliminarProveedor(int Id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
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
                await _context.DeleteEntityAsync(proveedor);
                await transaction.CommitAsync();
                return (true, null);

            }
            catch (Exception ex)
            {

                _logger.LogError("Error al eliminar el proveedor", ex);
                await transaction.RollbackAsync();
                return (false, "Error al eliminar el proveedor");
            }
           
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
               // proveedor.NombreProveedor = model.NombreProveedor;
               // proveedor.Contacto = model.Contacto;
               // proveedor.Direccion = model.Direccion;
               //await _context.UpdateEntityAsync(proveedor);
               await ActualizarProveedor(proveedor, model);
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
                //proveedor.NombreProveedor = model.NombreProveedor;
                //proveedor.Contacto = model.Contacto;
                //proveedor.Direccion = model.Direccion;
                await ActualizarProveedor(proveedor, model);
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError("Ocurrio una excepcion no esperada", ex);
                return (false, null);
            }
           
        }
        private async Task ActualizarProveedor(Proveedore proveedor, ProveedorViewModel model)
        {
            proveedor.NombreProveedor = model.NombreProveedor;
            proveedor.Contacto = model.Contacto;
            proveedor.Direccion = model.Direccion;
            await _context.UpdateEntityAsync(proveedor);
        }
    }
}
