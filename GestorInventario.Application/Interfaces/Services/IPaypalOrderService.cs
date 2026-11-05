using GestorInventario.Application.DTOS.Checkout;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface IPaypalOrderService
    {
        Task<OperationResult<CreateOrderResultDto>> CreateOrderWithPaypalAsync(CheckoutDto pagar);
        Task<OperationResult<PayPalOrderDetailsDto>> CapturarPagoAsync(string paymentId);
        Task<OperationResult<PayPalOrderDetailsDto>> ObtenerDetallesPagoEjecutadoAsync(string id);
      
    }
}
