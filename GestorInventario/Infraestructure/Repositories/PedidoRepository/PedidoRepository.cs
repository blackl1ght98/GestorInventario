using GestorInventario.Domain.Models;
using GestorInventario.enums.Pedido;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Paypal;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System.Collections.Generic;

namespace GestorInventario.Infraestructure.Repositories.PedidoRepository
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
        public async Task<Pedido> ObtenerNumeroPedido(RefundFormViewModel form)
        {
            var numeroPedido = await _context.Pedidos.Include(x=>x.PayPalPaymentCaptures).FirstOrDefaultAsync(p => p.NumeroPedido == form.NumeroPedido);    
            return  numeroPedido;
        }
        public async Task<Pedido?> ObtenerPedidoPendienteUsuarioAsync(int usuarioId)
        {
            return await _context.Pedidos
                .Where(p => p.IdUsuario == usuarioId &&
                            p.EstadoPedido == EstadoPedido.Pendiente.ToString())
                .OrderByDescending(p => p.FechaPedido)
                
                .FirstOrDefaultAsync();
        }
        public async Task<List<DetallePedido>> ObtenerDetallesPedidoAsync(int pedidoId)
        {
            return await _context.DetallePedidos
                .Where(d => d.PedidoId == pedidoId)
                .Include(d => d.Producto)  
                .ToListAsync();
        }
        public IQueryable<Pedido> ObtenerPedidos()=>
            from p in _context.Pedidos.Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation).Include(x=>x.PayPalPaymentCaptures)
            select p;
      
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)=>_context.Pedidos.Where(p => p.IdUsuario == userId).Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation);

        public async Task<DetallePedido> ObtenerDetallePorIdAsync(int id)
        {
            var detalle = await _context.DetallePedidos.FirstOrDefaultAsync(x => x.Id == id);
            return detalle;
        }
       
        public async Task<Pedido> ObtenerPedidoConDetallesAsync(int id) =>
          await _context.Pedidos
              .Include(p => p.DetallePedidos)
                  .ThenInclude(dp => dp.Producto)
              .Include(p => p.IdUsuarioNavigation)
               .Include(p=>p.PayPalPaymentCaptures)           
              .FirstOrDefaultAsync(m => m.Id == id);
        public async Task<Pedido> ObtenerPedidoPorIdAsync(int id) => await _context.Pedidos.FirstOrDefaultAsync(x => x.Id == id);
        public async Task<Pedido> ObtenerPedidoConCapturasAsync(int id) => await _context.Pedidos.Include(x=>x.PayPalPaymentCaptures).FirstOrDefaultAsync(x => x.Id == id);
       
        public async Task<DetallePedido> ObtenerDetalleParaReembolsoAsync(int detallePedidoId)
        {     
                var detalle = await _context.DetallePedidos
                    .Include(dp => dp.Producto)
                    .Include(dp => dp.Pedido)
                    .ThenInclude(x => x.PayPalPaymentCaptures)
                    
                    .FirstOrDefaultAsync(dp => dp.Id == detallePedidoId);
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
        public async Task<OperationResult<string>> EliminarCarritoAsync(Pedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await  _context.DeleteRangeEntityAsync(pedido.DetallePedidos);
                await _context.DeleteEntityAsync(pedido);
                return OperationResult<string>.Ok("Pedido eliminado con exito");
            });
        }
      

    }
    
}