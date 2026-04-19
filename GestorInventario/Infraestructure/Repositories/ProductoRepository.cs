using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class ProductoRepository : IProductoRepository
    {
        private readonly GestorInventarioContext _context;      
        private readonly ILogger<ProductoRepository> _logger;   
     
        public ProductoRepository(GestorInventarioContext context, 
        ILogger<ProductoRepository> logger)
        {
            _context = context;
                
            _logger = logger;
         
        }    
     
        public IQueryable<Producto> ObtenerTodosLosProductos()=>from p in _context.Productos.Include(x => x.IdProveedorNavigation)orderby p.Id  select p;
    
        
        public async Task<OperationResult<Producto>> AgregarProductoAsync(Producto producto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(producto);
                return OperationResult<Producto>.Ok("Producto creado", producto);
            });
        }
       
        public async Task<bool> ExisteProductoAsync(string nombre)
        {
            return await _context.Productos.AnyAsync(x => x.NombreProducto == nombre);
        }
        public async Task<List<Proveedore>> ObtenerProveedores()=>await _context.Proveedores.ToListAsync();
        public async Task<(Producto?,string)> ObtenerProductoPorId(int id)
        {
            var producto = await _context.Productos.Include(p => p.IdProveedorNavigation).FirstOrDefaultAsync(m => m.Id == id);
            return producto is null ? (null,"Producto no encontrado"): (producto,"Producto encontrado");
        }
        public async Task<Producto> ObtenerProductoCompletoAsync(int Id)
        {
            var producto = await _context.Productos
                   .Include(p => p.DetallePedidos)
                   .ThenInclude(dp => dp.Pedido)
                   .Include(p => p.IdProveedorNavigation)
                   .FirstOrDefaultAsync(m => m.Id == Id);
            return producto;
        }
        public async Task<OperationResult<Producto>> EliminarProductoAsync(Producto producto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(producto);
                return OperationResult<Producto>.Ok("Producto eliminado", producto);
            });
        }
        public async Task<OperationResult<Producto>> ActualizarProductoAsync(Producto producto)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(producto);
                return OperationResult<Producto>.Ok("Producto actualizado", producto);
            });
        }
        public async Task<OperationResult<DetallePedido>> ActualizarDetallePedidoAsync(DetallePedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(pedido);
                return OperationResult<DetallePedido>.Ok("Detalle del pedido actualizado", pedido);
            });
        }
        public async Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(pedido);
                return OperationResult<DetallePedido>.Ok("Detalle del producto actualizado", pedido);
            });
        }
        public async Task<DetallePedido?> ObtenerDetallesCarrito(int idCarrito, int idProducto)
        {
            return await _context.DetallePedidos
                .FirstOrDefaultAsync(d => d.PedidoId == idCarrito && d.ProductoId == idProducto);
        }

    }
}
