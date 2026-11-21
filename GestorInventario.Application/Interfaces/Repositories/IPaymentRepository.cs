using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Repositories
{
    public interface IPaymentRepository
    {


        //Consultas
       
        Task<PayPalPaymentDetails> ObtenerDetallesPago(string id);
     
        Task<PayPalPaymentDetails> ObtenerDetallesPagoPorIDAsync(string pagoId);
     
        //Operaciones
        Task<OperationResult<PayPalPaymentDetails>> AgregarDetallePagoAsync(PayPalPaymentDetails detalle);
        Task<OperationResult<PayPalPaymentItems>> AgregarPagoItemAsync(PayPalPaymentItems detalle);
        Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetails pago);
        Task<OperationResult<PayPalPaymentShipping>> AgregarInfoEnvioAsync(PayPalPaymentShipping detalle);
        Task<OperationResult<PayPalPaymentCaptures>> AgregarCaptureAsync(PayPalPaymentCaptures detalle);

        Task<OperationResult<PayPalPaymentRefunds>> AgregarRefundAsync(PayPalPaymentRefunds refund);

    }
}
