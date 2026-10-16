using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Shared.ExternalServices.Paypal.API.Requests.POST;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderTrackingService
    {
        Task<OperationResult<string>>
         AddTrackingAsync(
             string paymentId,
             string captureId,
             Carrier carrier,

             List<PayPalTrackingItemRequest> items);
    }
}
