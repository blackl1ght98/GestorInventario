using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Repositories.ProveedorRepository
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly GestorInventarioContext _context;
       
     
        public ProveedorRepository(GestorInventarioContext context)
        {
            _context = context;
           
           
        }

        public IQueryable<Proveedore> ObtenerProveedores()
        {
            return _context.Proveedores
                .Include(p => p.IdUsuarioNavigation); 
        }
        public async Task<Proveedore> ObtenerProveedorId(int id)
        {
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);         
            return  proveedor;
        }
        public async Task<Proveedore> ObtenerProveedorNombre(string nombreProveedor)
        {
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.NombreProveedor == nombreProveedor);         
            return  proveedor;
        }
        public async Task<Proveedore> ObtenerProveedoresConProductos(int Id)
        {
            var proveedor = await _context.Proveedores.Include(p => p.Productos).FirstOrDefaultAsync(m => m.Id == Id);         
            return  proveedor;
        }
        public async Task<OperationResult<Proveedore>> AddAsync(Proveedore proveedore)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(proveedore);
                return OperationResult<Proveedore>.Ok(proveedore);
            });
        }
        public async Task<OperationResult<Proveedore>> UpdateAsync(Proveedore proveedore)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(proveedore);
                return OperationResult<Proveedore>.Ok(proveedore);
            });
        }
        public async Task<OperationResult<Proveedore>> DeleteAsync(Proveedore proveedore)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(proveedore);
                return OperationResult<Proveedore>.Ok(proveedore);
            });
        }
      
    
    }

}

