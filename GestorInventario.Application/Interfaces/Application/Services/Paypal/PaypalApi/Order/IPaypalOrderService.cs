using GestorInventario.Shared.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Order;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderService
    {
        Task<OperationResult<string>> CreateOrderWithPaypalAsync(CheckoutRequest pagar);
        Task<OperationResult<OrderDetailsResponse>> CapturarPagoAsync(string orderId);
        Task<OperationResult<OrderDetailsResponse>> ObtenerDetallesPagoEjecutadoAsync(string id);
        Task<bool> VerificarReembolsoExistenteAsync(OrderDetailsResponse capture, decimal monto);
        string? ObtenerRefundIdPorMonto(OrderDetailsResponse capture, decimal monto);
    }
}
