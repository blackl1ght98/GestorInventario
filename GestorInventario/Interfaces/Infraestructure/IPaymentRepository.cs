using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.ViewModels.Paypal;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {



        Task<Pedido> BuscarPedidoCorrupto(int userId);
        Task<OperationResult<PayPalPaymentDetail>> AgregarDetallePagoAsync(PayPalPaymentDetail detalle);
        Task<OperationResult<PayPalPaymentItem>> AgregarPagoItemAsync(PayPalPaymentItem detalle);
        Task<PayPalPaymentDetail> ObtenerDetallesPago(string id);
        Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetail pago);
        Task<OperationResult<PayPalPaymentShipping>> AgregarInfoEnvioAsync(PayPalPaymentShipping detalle);
        Task<OperationResult<PayPalPaymentCapture>> AgregarCaptureAsync(PayPalPaymentCapture detalle);
    }
}
