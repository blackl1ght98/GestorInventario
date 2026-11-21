using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Infrastructure.Extensions;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Repositories.OrderRepository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly GestorInventarioContext _context;                   
              
     
        public OrderRepository(GestorInventarioContext context)
        {
            _context = context;      
                  
         
        }
        public async Task<Pedidos> ObtenerNumeroPedido(string numPedido)
        {
            var numeroPedido = await _context.Pedidos.Include(x=>x.PayPalPaymentCaptures).FirstOrDefaultAsync(p => p.NumeroPedido == numPedido);    
            return  numeroPedido;
        }
        public async Task<Pedidos?> ObtenerPedidoPendienteUsuarioAsync(int usuarioId)
        {
            return await _context.Pedidos
                .Where(p => p.IdUsuario == usuarioId &&
                            p.EstadoPedido == EstadoPedido.Pendiente.ToString())
                .OrderByDescending(p => p.FechaPedido)
                
                .FirstOrDefaultAsync();
        }
        public async Task<List<DetallePedido>> ObtenerDetallesPedidoAsync(int pedidoId)
        {
            return await _context.DetallePedido
                .Where(d => d.PedidoId == pedidoId)
                .Include(d => d.Producto)  
                .ToListAsync();
        }
        public IQueryable<Pedidos> ObtenerPedidos()=>
            from p in _context.Pedidos.Include(dp => dp.DetallePedido).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation).Include(x=>x.PayPalPaymentCaptures)
            select p;
      
        public IQueryable<Pedidos> ObtenerPedidoUsuario(int userId)=>_context.Pedidos.Where(p => p.IdUsuario == userId).Include(dp => dp.DetallePedido).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation).Include(x=>x.PayPalPaymentCaptures);

        public async Task<DetallePedido> ObtenerDetallePorIdAsync(int id)
        {
            var detalle = await _context.DetallePedido.FirstOrDefaultAsync(x => x.Id == id);
            return detalle;
        }
       
        public async Task<Pedidos> ObtenerPedidoConDetallesAsync(int id) =>
          await _context.Pedidos
              .Include(p => p.DetallePedido)
                  .ThenInclude(dp => dp.Producto)
              .Include(p => p.IdUsuarioNavigation)
               .Include(p=>p.PayPalPaymentCaptures)           
              .FirstOrDefaultAsync(m => m.Id == id);
        public async Task<Pedidos> ObtenerPedidoPorIdAsync(int id) => await _context.Pedidos.FirstOrDefaultAsync(x => x.Id == id);
        public async Task<Pedidos> ObtenerPedidoConCapturasAsync(int id) => await _context.Pedidos.Include(x=>x.PayPalPaymentCaptures).FirstOrDefaultAsync(x => x.Id == id);
       
        public async Task<DetallePedido> ObtenerDetalleParaReembolsoAsync(int detallePedidoId)
        {     
                var detalle = await _context.DetallePedido
                    .Include(dp => dp.Producto)
                    .Include(dp => dp.Pedido)
                    
                    .ThenInclude(x => x.PayPalPaymentCaptures)
                    
                    .FirstOrDefaultAsync(dp => dp.Id == detallePedidoId);
                return detalle;
         
        }
        public async Task<OperationResult<Pedidos>> AgregarPedidoAsync(Pedidos pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(pedido);
                return OperationResult<Pedidos>.Ok( pedido);
            });
        }
        public async Task<OperationResult<Pedidos>> ActualizarPedidoAsync(Pedidos pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(pedido);
                return OperationResult<Pedidos>.Ok( pedido);
            });
        }
        public async Task<OperationResult<DetallePedido>> ActualizarDetallePedidoAsync(DetallePedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.UpdateEntityAsync(pedido);
                return OperationResult<DetallePedido>.Ok( pedido);
            });
        }
        public async Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(pedido);
                return OperationResult<DetallePedido>.Ok( pedido);
            });
        }
        public async Task<OperationResult<DetallePedido>> EliminarDetallePedidoAsync(DetallePedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(pedido);
                return OperationResult<DetallePedido>.Ok( pedido);
            });
        }
        public async Task<OperationResult<string>> EliminarCarritoAsync(Pedidos pedido)
        {
            // Si los detalles no vienen cargados, los buscamos primero
            if (pedido.DetallePedido == null || !pedido.DetallePedido.Any())
            {
                pedido.DetallePedido = await _context.DetallePedido
                    .Where(dp => dp.PedidoId == pedido.Id)
                    .ToListAsync();
            }
            
            await _context.DeleteWithChildrenAsync(pedido, pedido.DetallePedido);

            return OperationResult<string>.Ok("Pedido eliminado con exito");
        }
        public async Task<List<Pedidos>> ObtenerTodosLosPendientesAntiguosAsync(DateTime limiteTiempo)
        {
            return await _context.Pedidos
                .Include(p => p.DetallePedido) 
                .Where(p => p.EstadoPedido == EstadoPedido.Pendiente.ToString() 
                            && p.FechaPedido <= limiteTiempo)
                .ToListAsync();
        }
    }
    
}