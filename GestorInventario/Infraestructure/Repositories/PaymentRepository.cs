using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Utils;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.Paypal;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GestorInventario.Infraestructure.Repositories
{
    public class PaymentRepository:IPaymentRepository
    {
        public readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalRepository> _logger;
   
        public PaymentRepository(GestorInventarioContext context, ILogger<PaypalRepository> logger)
        {
            _context = context;
            _logger = logger;
         
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
        public async Task<PayPalPaymentDetail> ObtenerDetallesPago(string id) => await _context.PayPalPaymentDetails.Include(d => d.PayPalPaymentItems).FirstOrDefaultAsync(x => x.Id == id);

        public async Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetail pago)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                _context.DeleteRangeEntity(pago.PayPalPaymentItems);

                return OperationResult<string>.Ok("Pedido eliminado con exito");
            });
        }
       public async Task<Pedido> BuscarPedidoCorrupto(int userId)=> await _context.Pedidos.Include(x => x.DetallePedidos).Where(p => p.IdUsuario == userId && p.EstadoPedido == EstadoPedido.En_Proceso.ToString() &&
            string.IsNullOrEmpty(p.CaptureId)).FirstOrDefaultAsync();


    }
}
