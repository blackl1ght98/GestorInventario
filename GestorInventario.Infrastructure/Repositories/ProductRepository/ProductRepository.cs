using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Infrastructure.Extensions;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Repositories.ProductRepository
{
    public class ProductRepository : IProductRepository
    {
        private readonly GestorInventarioContext _context;      
         
     
        public ProductRepository(GestorInventarioContext context)
        {
            _context = context;              
                
        }    
     
        public IQueryable<Productos> ObtenerTodosLosProductos()=>from p in _context.Productos.Include(x => x.IdProveedorNavigation)orderby p.Id  select p;

        public async Task<bool> ObtenerCodigoUPC(string code) => await _context.Productos.AnyAsync(p => p.CodigoBarras == code);
        public async Task<OperationResult<Productos>> AgregarProductoAsync(Productos producto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(producto);
                return OperationResult<Productos>.Ok( producto);
            });
        }
       
        public async Task<bool> ExisteNombreProductoAsync(string nombre)
        {
            return await _context.Productos.AnyAsync(x => x.NombreProducto == nombre);
        }
        public async Task<List<Proveedores>> ObtenerProveedores()=>await _context.Proveedores.ToListAsync();
       
        public async Task<Productos> ObtenerProductoPorIdAsync(int id)
        {
            var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
            return producto;
        }
        public async Task<Productos> ObtenerProductoCompletoAsync(int Id)
        {
            var producto = await _context.Productos
                   .Include(p => p.DetallePedido)
                   .ThenInclude(dp => dp.Pedido)
                   .Include(p => p.IdProveedorNavigation)
                   .FirstOrDefaultAsync(m => m.Id == Id);
            return producto;
        }
        public async Task<DetallePedido> ObtenerDetallesCarrito(int idCarrito, int idProducto)
        {
            var detallesCarrito = await _context.DetallePedido
                .FirstOrDefaultAsync(d => d.PedidoId == idCarrito && d.ProductoId == idProducto);
            return  detallesCarrito;
        }
        public async Task<OperationResult<Productos>> EliminarProductoAsync(Productos producto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(producto);
                return OperationResult<Productos>.Ok( producto);
            });
        }
        public async Task<OperationResult<Productos>> ActualizarProductoAsync(Productos producto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(producto);
                return OperationResult<Productos>.Ok( producto);
            });
        }
    }
}
