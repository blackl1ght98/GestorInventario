using GestorInventario.Shared.DTOS.Checkout;
using GestorInventario.Shared.DTOS.Paypal.BD;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderService
    {
        Task<OperationResult<string>> CreateOrderWithPaypalAsync(CheckoutDto pagar);
        Task<OperationResult<PaymentCapturedDto>> CapturarPagoAsync(string orderId);
        Task<OperationResult<OrderDetailsResponse>> ObtenerDetallesPagoEjecutadoAsync(string id);
    }
}
