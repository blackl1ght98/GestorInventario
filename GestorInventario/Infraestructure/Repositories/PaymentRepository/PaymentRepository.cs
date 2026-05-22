using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.MetodosExtension;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infraestructure.Repositories.PaymentRepository
{
    public class PaymentRepository:IPaymentRepository
    {
        public readonly GestorInventarioContext _context;
        private readonly ILogger<PaymentRepository> _logger;
   
        public PaymentRepository(GestorInventarioContext context, ILogger<PaymentRepository> logger)
        {
            _context = context;
            _logger = logger;
         
        }
      
       public async Task<PayPalPaymentDetail> ObtenerDetallesPagoPorIDAsync(string pagoId)=> await _context.PayPalPaymentDetails
                .Include(d => d.PayPalPaymentItems)
                .Include(d => d.PayPalPaymentCaptures)
                .Include(d => d.PayPalPaymentShippings)
                .FirstOrDefaultAsync(d => d.Id == pagoId);
        public async Task<Pedido> BuscarPedidoCorrupto(int userId) => await _context.Pedidos.Include(x => x.DetallePedidos).Where(p => p.IdUsuario == userId && p.EstadoPedido == EstadoPedido.En_Proceso.ToString() &&
          string.IsNullOrEmpty(p.CaptureId)).FirstOrDefaultAsync();
        public async Task<PayPalPaymentDetail> ObtenerDetallesPago(string id) => await _context.PayPalPaymentDetails.Include(d => d.PayPalPaymentItems).Include(x => x.PayPalPaymentShippings).Include(x => x.PayPalPaymentCaptures).FirstOrDefaultAsync(x => x.Id == id);
        public async Task<PayPalPaymentCapture?> ObtenerCapturePorCaptureIdAsync(string captureId)
        {
            var capture = await _context.PayPalPaymentCaptures.FirstOrDefaultAsync(x=>x.CaptureId==captureId);
            return capture;
        }
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
     
        public async Task<OperationResult<PayPalPaymentShipping>> AgregarInfoEnvioAsync(PayPalPaymentShipping detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentShipping>.Ok("", detalle);
            });
        }
        public async Task<OperationResult<PayPalPaymentCapture>> AgregarCaptureAsync(PayPalPaymentCapture detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentCapture>.Ok("", detalle);
            });
        }

        public async Task<OperationResult<PayPalPaymentCapture>> EliminarCapturesAsync(PayPalPaymentCapture detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteEntityAsync(detalle);
                return OperationResult<PayPalPaymentCapture>.Ok("", detalle);
            });
        }
        
        public async Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetail pago)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteRangeEntityAsync(pago.PayPalPaymentItems);
                await _context.DeleteRangeEntityAsync(pago.PayPalPaymentCaptures);
                await _context.DeleteRangeEntityAsync(pago.PayPalPaymentShippings);


                return OperationResult<string>.Ok("Pedido eliminado con exito");
            });
        }
     

    }
}
