using GestorInventario.Application.DTOS;
using GestorInventario.Application.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Application.ExternalServices.Paypal.API.Responses.GET.Order;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderService
    {
        Task<OperationResult<string>> CreateOrderWithPaypalAsync(CheckoutRequest pagar);
        Task<OperationResult<OrderDetailsResponse>> CapturarPagoAsync(string orderId);
        Task<OperationResult<PayPalOrderDetailsDto>> ObtenerDetallesPagoEjecutadoAsync(string id);
      
    }
}
