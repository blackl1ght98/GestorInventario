using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class CarritoRepository : ICarritoRepository
    {
        private readonly GestorInventarioContext _context;     
        private readonly ILogger<CarritoRepository> _logger;         
      
        public CarritoRepository(GestorInventarioContext context,  
            ILogger<CarritoRepository> logger )
        {
            _context = context;        
            _logger = logger;                
           
        }

        // Obtener el carrito del usuario (Pedidos con EsCarrito = 1)
        public async Task<OperationResult<Pedido>> ObtenerCarritoUsuario(int userId)
        {
            var carrito = await _context.Pedidos
                .FirstOrDefaultAsync(x => x.IdUsuario == userId && x.EsCarrito);
            if (carrito == null) {
                return OperationResult<Pedido>.Fail("No se puede obtener el carrito del usuario");
            }
            return OperationResult<Pedido>.Ok("Carrito obtenido con exito", carrito);
            
        }
        public async Task<List<Pedido>> ObtenerCarritosActivosAsync(int userId)=> await _context.Pedidos
               .Include(p => p.DetallePedidos)
               .Where(p => p.IdUsuario == userId && p.EsCarrito)
               .ToListAsync();

        // Obtener ítems del carrito (DetallePedido para un Pedido con EsCarrito = 1)
        public async Task<OperationResult<List<DetallePedido>>> ObtenerItemsDelCarritoUsuario(int pedidoId)
        {
            var detallePedido = await _context.DetallePedidos
                .Where(i => i.PedidoId == pedidoId)
                .ToListAsync();
            return OperationResult<List<DetallePedido>>.Ok("", detallePedido);
        }

        // Obtener un ítem específico del carrito por ID
        public async Task<OperationResult<DetallePedido>> ItemsDelCarrito(int id)
        {
            var item = await _context.DetallePedidos
                .FirstOrDefaultAsync(x => x.Id == id);

            if (item is null)
                return OperationResult<DetallePedido>.Fail("No hay productos en el carrito");

            return OperationResult<DetallePedido>.Ok("Productos obtenidos",item);
        }


        // Obtener ítems con datos relacionados (producto y proveedor)
        public OperationResult<IQueryable<DetallePedido>> ObtenerItemsConDetalles(int pedidoId)
        {
            var detalle = _context.DetallePedidos
                .Include(i => i.Producto)
                .Include(i => i.Producto.IdProveedorNavigation)
                .Where(i => i.PedidoId == pedidoId);
            return OperationResult<IQueryable<DetallePedido>>.Ok("", detalle);
        }

        // Obtener monedas disponibles
        public async Task<OperationResult<List<Monedum>>> ObtenerMoneda()
        {
            var monedas = await _context.Moneda.ToListAsync();
            return OperationResult<List<Monedum>>.Ok("",monedas);
        }    
      
    }
}