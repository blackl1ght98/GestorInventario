using GestorInventario.Domain.Models;

using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;
using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrastructure.Repositories.PaymentRepository
{
    public class PaymentRepository:IPaymentRepository
    {
        public readonly GestorInventarioContext _context;
     
   
        public PaymentRepository(GestorInventarioContext context)
        {
            _context = context;
          
         
        }
    
        public async Task<PayPalPaymentDetail> ObtenerDetallesPagoPorIDAsync(string pagoId)=> await _context.PayPalPaymentDetails
                .Include(d => d.PayPalPaymentItems)
                .Include(d => d.PayPalPaymentCaptures)
                .Include(d => d.PayPalPaymentShippings)
                .FirstOrDefaultAsync(d => d.Id == pagoId);
      
        public async Task<PayPalPaymentDetail> ObtenerDetallesPago(string id) => await _context.PayPalPaymentDetails.Include(d => d.PayPalPaymentItems).Include(x => x.PayPalPaymentShippings).Include(x => x.PayPalPaymentCaptures).FirstOrDefaultAsync(x => x.Id == id);
       
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

       
        // METODO USADO SOLO EN SINCRONIZACION DE DATOS CON PAYPAL
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
