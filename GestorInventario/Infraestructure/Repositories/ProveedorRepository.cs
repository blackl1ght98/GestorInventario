using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.provider;
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
            return _context.Proveedores
                .Include(p => p.IdUsuarioNavigation); 
        }
        public async Task<List<Usuario>> ObtenerProveedoresLista()
        {
            return await _context.Usuarios.ToListAsync();
        }

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
                    IdUsuario=model.IdUsuario
                };
                await _context.AddEntityAsync(proveedor);
                await transaction.CommitAsync();
                return (true, "Proveedor creado con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex,"Error al crear el proveedor");
                await transaction.RollbackAsync();
                return (false, "Error al crear el proveedor");
            }
           
        }
        public async Task<(Proveedore?,string)> ObtenerProveedorId(int id)
        {
            var proveedor= await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);
            return proveedor is null ? (null, "Proveedor no encontrado") : (proveedor, "Proveedor encontrado");
        }

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
                return (true, "Proveedor eliminado con exito");

            }
            catch (Exception ex)
            {

                _logger.LogError(ex,"Error al eliminar el proveedor");
                await transaction.RollbackAsync();
                return (false, "Error al eliminar el proveedor");
            }
           
        }
        public async Task<(bool, string)> EditarProveedor(ProveedorViewModel model, int Id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var proveedor = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == Id); 
                if (proveedor == null)
                {
                    return (false, "El proveedor no existe");
                }

                await ActualizarProveedor(proveedor, model);
                await transaction.CommitAsync();
                return (true, "Proveedor editado con exito");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error de concurrencia");
                var proveedor = await _context.Proveedores.FirstOrDefaultAsync(x => x.Id == Id); // Removed Include
                if (proveedor == null)
                {
                    return (false, "El proveedor no existe");
                }
                _context.Entry(proveedor).Reload();
                _context.Entry(proveedor).State = EntityState.Modified;

                await ActualizarProveedor(proveedor, model);
                await transaction.CommitAsync();
                return (true, "Proveedor editado con exito");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ocurrio un error inesperado al editar el proveedor");
                return (false, "Ocurrio un error inesperado al editar el proveedor");
            }

        }

        private async Task ActualizarProveedor(Proveedore proveedor, ProveedorViewModel model) 
        {
            proveedor.NombreProveedor = model.NombreProveedor;
            proveedor.Contacto = model.Contacto;
            proveedor.Direccion = model.Direccion;
            proveedor.IdUsuario = model.IdUsuario;
            await _context.UpdateEntityAsync(proveedor);
          
        }
    }
}
