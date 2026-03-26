using GestorInventario.Application.DTOs.Checkout;
using GestorInventario.Application.DTOs.Response_paypal.GET;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalOrderService
    {
        Task<string> CreateOrderWithPaypalAsync(CheckoutDto pagar);
        Task<(string CaptureId, string Total, string Currency)> CapturarPagoAsync(string orderId);
        Task<CheckoutDetailsDto> ObtenerDetallesPagoEjecutadoV2(string id);

    }
}
