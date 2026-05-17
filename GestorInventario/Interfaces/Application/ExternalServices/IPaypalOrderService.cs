using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;


namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalOrderService
    {
        Task<string> CreateOrderWithPaypalAsync(CheckoutDto pagar);
        Task<(string CaptureId, string Total, string Currency)> CapturarPagoAsync(string orderId);
        Task<OrderDetailsResponse> ObtenerDetallesPagoEjecutadoAsync(string id);
    }
}
