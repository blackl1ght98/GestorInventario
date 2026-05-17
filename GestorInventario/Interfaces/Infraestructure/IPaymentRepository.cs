
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;


namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IPaymentRepository
    {


        //Consultas
        Task<Pedido> BuscarPedidoCorrupto(int userId);
        Task<PayPalPaymentDetail> ObtenerDetallesPago(string id);
        Task<PayPalPaymentDetail> ObtenerDetallesPagoPorIDAsync(string pagoId);
        //Operaciones
        Task<OperationResult<PayPalPaymentDetail>> AgregarDetallePagoAsync(PayPalPaymentDetail detalle);
        Task<OperationResult<PayPalPaymentItem>> AgregarPagoItemAsync(PayPalPaymentItem detalle);
        Task<OperationResult<string>> EliminarDetallesPagoAsync(PayPalPaymentDetail pago);
        Task<OperationResult<PayPalPaymentShipping>> AgregarInfoEnvioAsync(PayPalPaymentShipping detalle);
        Task<OperationResult<PayPalPaymentCapture>> AgregarCaptureAsync(PayPalPaymentCapture detalle);
       
    }
}
