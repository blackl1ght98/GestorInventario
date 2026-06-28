using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrestructura.Repositories.CarritoRepository
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
        public async Task<Pedido> ObtenerCarritoUsuario(int userId)
        {
            var carrito = await _context.Pedidos
                .FirstOrDefaultAsync(x => x.IdUsuario == userId && x.EsCarrito);
            return carrito;
            
        }
        public async Task<List<Pedido>> ObtenerCarritosActivosAsync(int userId)=> await _context.Pedidos
               .Include(p => p.DetallePedidos)
               .Where(p => p.IdUsuario == userId && p.EsCarrito)
               .ToListAsync();

        // Obtener ítems del carrito (DetallePedido para un Pedido con EsCarrito = 1)
        public async Task<List<DetallePedido>> ObtenerItemsDelCarritoUsuario(int pedidoId)
        {
            var detallePedido = await _context.DetallePedidos
                .Where(i => i.PedidoId == pedidoId)
                .ToListAsync();
            return  detallePedido;
        }

        // Obtener un ítem específico del carrito por ID
        public async Task<DetallePedido> ItemsDelCarrito(int id)
        {
            var item = await _context.DetallePedidos
                .FirstOrDefaultAsync(x => x.Id == id);
            return item;
        }


        // Obtener ítems con datos relacionados (producto y proveedor)
        public IQueryable<DetallePedido> ObtenerItemsConDetalles(int pedidoId)
        {
            var detalle = _context.DetallePedidos
                .Include(i => i.Producto)
                .Include(i => i.Producto.IdProveedorNavigation)
                .Where(i => i.PedidoId == pedidoId);
            return  detalle;
        }

        // Obtener monedas disponibles
        public async Task<List<Monedum>> ObtenerMoneda()
        {
            var monedas = await _context.Moneda.ToListAsync();
            return monedas;
        }    
      
    }
}