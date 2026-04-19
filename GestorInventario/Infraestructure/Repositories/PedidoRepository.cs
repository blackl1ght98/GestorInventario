
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Utils;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.order;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using System.Globalization;
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
   
        public IQueryable<Pedido> ObtenerPedidos()=>
            from p in _context.Pedidos.Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation)
            select p;
      
        public IQueryable<Pedido> ObtenerPedidoUsuario(int userId)=>_context.Pedidos.Where(p => p.IdUsuario == userId).Include(dp => dp.DetallePedidos).ThenInclude(p => p.Producto).Include(u => u.IdUsuarioNavigation);


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
                return OperationResult<Pedido>.Ok("Pedido Agregado", pedido);
            });
        }
        public async Task<OperationResult<DetallePedido>> AgregarDetallePedidoAsync(DetallePedido detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<DetallePedido>.Ok("Pedido Agregado", detalle);
            });
        }
        public async Task<OperationResult<string>> EliminarPedidoAsync(Pedido pedido)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.DeleteRangeEntity(pedido.DetallePedidos);
                _context.DeleteRangeEntity(pedido.Rembolsos);
                _context.DeleteEntity(pedido);
                return OperationResult<string>.Ok("Pedido eliminado con exito");
            });
        }
        public async Task<Pedido> ObtenerPedidoConDetallesAsync(int id) =>
            await _context.Pedidos
                .Include(p => p.DetallePedidos)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.IdUsuarioNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<Pedido> ObtenerPedidoPorId(int id)=> await _context.Pedidos.FirstOrDefaultAsync(x => x.Id == id);               
       
       
        public async Task<Pedido> ObtenerPedidoConRembolso(int id) => await _context.Pedidos.Include(p => p.DetallePedidos).Include(x => x.Rembolsos).FirstOrDefaultAsync(m => m.Id == id);
    
        public async Task<PayPalPaymentDetail> ObtenerDetallesPago(string id) => await _context.PayPalPaymentDetails.Include(d => d.PayPalPaymentItems).FirstOrDefaultAsync(x => x.Id == id);
        public async Task<OperationResult<PayPalPaymentDetail>> AgregarDetallePagoAsync(PayPalPaymentDetail detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentDetail>.Ok("Pedido Agregado", detalle);
            });
        }
        public async Task<OperationResult<PayPalPaymentItem>> AgregarPagoItemAsync(PayPalPaymentItem detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentItem>.Ok("", detalle);
            });
        }
        public async Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetail pago)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.DeleteRangeEntity(pago.PayPalPaymentItems);

                return OperationResult<string>.Ok("Pedido eliminado con exito");
            });
        }
       
     
       
    }
    
}