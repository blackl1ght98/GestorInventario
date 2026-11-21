using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Infrastructure.Extensions;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Repositories.ProviderRepository
{
    public class ProviderRepository : IProviderRepository
    {
        private readonly GestorInventarioContext _context;
       
     
        public ProviderRepository(GestorInventarioContext context)
        {
            _context = context;
           
           
        }

        public IQueryable<Proveedores> ObtenerProveedores()
        {
            return _context.Proveedores
                .Include(p => p.IdUsuarioNavigation); 
        }
        public async Task<Proveedores> ObtenerProveedorId(int id)
        {
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.Id == id);         
            return  proveedor;
        }
        public async Task<Proveedores> ObtenerProveedorNombre(string nombreProveedor)
        {
            var proveedor = await _context.Proveedores.FirstOrDefaultAsync(m => m.NombreProveedor == nombreProveedor);         
            return  proveedor;
        }
        public async Task<Proveedores> ObtenerProveedoresConProductos(int Id)
        {
            var proveedor = await _context.Proveedores.Include(p => p.Productos).FirstOrDefaultAsync(m => m.Id == Id);         
            return  proveedor;
        }
        public async Task<OperationResult<Proveedores>> AddAsync(Proveedores proveedore)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(proveedore);
                return OperationResult<Proveedores>.Ok(proveedore);
            });
        }
        public async Task<OperationResult<Proveedores>> UpdateAsync(Proveedores proveedore)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(proveedore);
                return OperationResult<Proveedores>.Ok(proveedore);
            });
        }
        public async Task<OperationResult<Proveedores>> DeleteAsync(Proveedores proveedore)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(proveedore);
                return OperationResult<Proveedores>.Ok(proveedore);
            });
        }
      
    
    }

}

