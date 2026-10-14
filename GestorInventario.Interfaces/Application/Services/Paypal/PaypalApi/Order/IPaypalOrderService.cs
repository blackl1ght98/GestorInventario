using GestorInventario.Shared.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Shared.ExternalServices.Paypal.API.Responses.GET.Order;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderService
    {
        Task<OperationResult<string>> CreateOrderWithPaypalAsync(CheckoutRequest pagar);
        Task<OperationResult<OrderDetailsResponse>> CapturarPagoAsync(string orderId);
        Task<OperationResult<OrderDetailsResponse>> ObtenerDetallesPagoEjecutadoAsync(string id);
    }
}
