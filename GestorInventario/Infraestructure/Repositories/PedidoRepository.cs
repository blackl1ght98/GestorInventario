
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Paypal;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly GestorInventarioContext _context;              
      
        private readonly ILogger<PedidoRepository> _logger;        
     
        public PedidoRepository(GestorInventarioContext context, 
         ILogger<PedidoRepository> logger )
        {
            _context = context;
        
            _logger = logger;            
         
        }
        public async Task<OperationResult<Pedido>> ObtenerNumeroPedido(RefundFormViewModel form)
        {
            var numeroPedido = await _context.Pedidos.FirstOrDefaultAsync(p => p.NumeroPedido == form.NumeroPedido);
            if (numeroPedido is null)
            {
                return OperationResult<Pedido>.Fail("El numero de pedido no se encuentra");
            }
            return OperationResult<Pedido>.Ok("", numeroPedido);
        }
        public async Task<Pedido?> ObtenerPedidoEnProcesoUsuarioAsync(int usuarioId)
        {
            return await _context.Pedidos
                .Where(p => p.IdUsuario == usuarioId &&
                            p.EstadoPedido == EstadoPedido.En_Proceso.ToString())
                .OrderByDescending(p => p.FechaPedido)
                .FirstOrDefaultAsync();
        }
       
        public IQueryable<Pedido> ObtenerPedidos()=>
            from p in _context.Pedidos.Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation)
            select p;
      
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)=>_context.Pedidos.Where(p => p.IdUsuario == userId).Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation);

        public async Task<DetallePedido> ObtenerDetallePorIdAsync(int id)
        {
            var detalle = await _context.DetallePedidos.FirstOrDefaultAsync(x => x.Id == id);
            return detalle;
        }
        public async Task<OperationResult<Pedido>> AgregarPedidoAsync(Pedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(pedido);
                return OperationResult<Pedido>.Ok("Pedido Agregado", pedido);
            });
        }
        public async Task<OperationResult<Pedido>> ActualizarPedidoAsync(Pedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(pedido);
                return OperationResult<Pedido>.Ok("Pedido actualizado", pedido);
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
        public async Task<OperationResult<DetallePedido>> EliminarDetallePedidoAsync(DetallePedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(pedido);
                return OperationResult<DetallePedido>.Ok("Detalle del producto eliminado", pedido);
            });
        }
        public async Task<OperationResult<string>> EliminarPedidoAsync(Pedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await  _context.DeleteRangeEntityAsync(pedido.DetallePedidos);
                await _context.DeleteRangeEntityAsync(pedido.Rembolsos);
                await _context.DeleteEntityAsync(pedido);
                return OperationResult<string>.Ok("Pedido eliminado con exito");
            });
        }
        public async Task<Pedido> ObtenerPedidoPorIdAsync(int id) =>
            await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<Pedido> ObtenerPedidoPorId(int id)=> await _context.Pedidos.FirstOrDefaultAsync(x => x.Id == id);               
       
       
        public async Task<Pedido> ObtenerPedidoConRembolso(int id) => await _context.Pedidos.Include(p => p.DetallePedidos).Include(x => x.Rembolsos).FirstOrDefaultAsync(m => m.Id == id);
        public async Task<(Pedido Pedido, decimal TotalAmount)> GetPedidoWithDetailsAsync(int pedidoId)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.CaptureId))
                    throw new ArgumentException("Pedido no encontrado o SaleId no disponible.");

                if (string.IsNullOrEmpty(pedido.Currency))
                    throw new ArgumentException("El código de moneda no está definido.");

                decimal totalAmount = pedido.DetallePedidos.Sum(d => d.Producto.Precio * (d.Cantidad ?? 0));

                return (pedido, totalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el pedido {pedidoId}");
                throw;
            }
        }
        public async Task<(DetallePedido Detalle, decimal PrecioProducto)> GetProductoDePedidoAsync(int detallePedidoId)
        {
            try
            {
                var detalle = await _context.DetallePedidos
                    .Include(dp => dp.Producto)
                    .Include(dp => dp.Pedido)
                    .FirstOrDefaultAsync(dp => dp.Id == detallePedidoId);

                if (detalle == null)
                    throw new ArgumentException("Detalle de pedido no encontrado");

                if (detalle.Producto == null)
                    throw new ArgumentException("Producto no encontrado en el detalle");

                return (detalle, detalle.Producto.Precio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el detalle de pedido {detallePedidoId}");
                throw;
            }
        }
        public async Task<(Pedido? Pedido, List<DetallePedido>? Detalles)> GetPedidoConDetallesAsync(int pedidoId)
        {
            try
            {
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallePedidos)
                    .ThenInclude(d => d.Producto)
                    .FirstOrDefaultAsync(p => p.Id == pedidoId);

                if (pedido == null || string.IsNullOrEmpty(pedido.CaptureId))
                {
                    _logger.LogWarning($"Pedido {pedidoId} no encontrado o sin SaleId");
                    return (null, null);
                }

                if (string.IsNullOrEmpty(pedido.Currency))
                {
                    _logger.LogWarning($"Pedido {pedidoId} sin moneda definida");
                    return (null, null);
                }

                return (pedido, pedido.DetallePedidos?.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener el pedido {pedidoId}");
                throw;
            }
        }

    }
    
}