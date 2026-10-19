using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Checkout;
using GestorInventario.Application.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderService
    {
        Task<OperationResult<CreateOrderResultDto>> CreateOrderWithPaypalAsync(CheckoutDto pagar);
        Task<OperationResult<PayPalOrderDetailsDto>> CapturarPagoAsync(string orderId);
        Task<OperationResult<PayPalOrderDetailsDto>> ObtenerDetallesPagoEjecutadoAsync(string id);
      
    }
}
