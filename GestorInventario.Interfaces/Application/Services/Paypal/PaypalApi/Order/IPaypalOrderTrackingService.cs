using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Shared.DTOS.Paypal.BD;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderTrackingService
    {
        Task<OperationResult<string>>
         AddTrackingAsync(
             string paymentId,
             string captureId,
             Carrier carrier,

             List<TrackingItemDto> items);
    }
}
