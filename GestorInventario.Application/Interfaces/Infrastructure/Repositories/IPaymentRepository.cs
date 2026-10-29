using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Infrastructure.Repositories
{
    public interface IPaymentRepository
    {


        //Consultas
       
        Task<PayPalPaymentDetail> ObtenerDetallesPago(string id);
     
        Task<PayPalPaymentDetail> ObtenerDetallesPagoPorIDAsync(string pagoId);
     
        //Operaciones
        Task<OperationResult<PayPalPaymentDetail>> AgregarDetallePagoAsync(PayPalPaymentDetail detalle);
        Task<OperationResult<PayPalPaymentItem>> AgregarPagoItemAsync(PayPalPaymentItem detalle);
       
        Task<OperationResult<PayPalPaymentShipping>> AgregarInfoEnvioAsync(PayPalPaymentShipping detalle);
        Task<OperationResult<PayPalPaymentCapture>> AgregarCaptureAsync(PayPalPaymentCapture detalle);

        Task<OperationResult<PayPalPaymentRefund>> AgregarRefundAsync(PayPalPaymentRefund refund);

    }
}
