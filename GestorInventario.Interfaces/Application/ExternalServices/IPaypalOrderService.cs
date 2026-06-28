using GestorInventario.Shared.DTOS.Checkout;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;


namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalOrderService
    {
        Task<string> CreateOrderWithPaypalAsync(CheckoutDto pagar);
        Task<(string CaptureId, decimal Total, string Currency)> CapturarPagoAsync(string orderId);
        Task<OrderDetailsResponse> ObtenerDetallesPagoEjecutadoAsync(string id);
    }
}
