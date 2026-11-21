using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Domain.Models;
using GestorInventario.Infrastructure.Data;
using GestorInventario.Infrastructure.Extensions;
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
    
        public async Task<PayPalPaymentDetails> ObtenerDetallesPagoPorIDAsync(string pagoId)=> await _context.PayPalPaymentDetails
                .Include(d => d.PayPalPaymentItems)
                .Include(d => d.PayPalPaymentCaptures)
                .Include(d => d.PayPalPaymentShipping)
                .FirstOrDefaultAsync(d => d.Id == pagoId);
      
        public async Task<PayPalPaymentDetails> ObtenerDetallesPago(string id) => await _context.PayPalPaymentDetails.Include(d => d.PayPalPaymentItems).Include(x => x.PayPalPaymentShipping).Include(x => x.PayPalPaymentCaptures).Include(x=>x.PayPalPaymentRefunds).FirstOrDefaultAsync(x => x.Id == id);
       
        public async Task<OperationResult<PayPalPaymentDetails>> AgregarDetallePagoAsync(PayPalPaymentDetails detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentDetails>.Ok( detalle);
            });
        }
        public async Task<OperationResult<PayPalPaymentItems>> AgregarPagoItemAsync(PayPalPaymentItems detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentItems>.Ok( detalle);
            });
        }
     
        public async Task<OperationResult<PayPalPaymentShipping>> AgregarInfoEnvioAsync(PayPalPaymentShipping detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentShipping>.Ok(detalle);
            });
        }
        public async Task<OperationResult<PayPalPaymentCaptures>> AgregarCaptureAsync(PayPalPaymentCaptures detalle)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(detalle);
                return OperationResult<PayPalPaymentCaptures>.Ok( detalle);
            });
        }
        
        public async Task<OperationResult<PayPalPaymentRefunds>> AgregarRefundAsync(PayPalPaymentRefunds refund)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(refund);
                return OperationResult<PayPalPaymentRefunds>.Ok( refund);
            });
        }
        public async Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetails pago)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.DeleteRangeEntityAsync(pago.PayPalPaymentItems);
                await _context.DeleteRangeEntityAsync(pago.PayPalPaymentCaptures);
                await _context.DeleteRangeEntityAsync(pago.PayPalPaymentShipping);
                await _context.DeleteRangeEntityAsync(pago.PayPalPaymentRefunds);


                return OperationResult<string>.Ok("Pedido eliminado con exito");
            });
        }
    }
}
