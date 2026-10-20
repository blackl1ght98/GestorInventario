using GestorInventario.Application.DTOS;

using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order
{
    public interface IPaypalOrderTrackingService
    {
        Task<OperationResult<string  >> 
            AddTrackingAsync(
                string paymentId,
                string captureId,
                Carrier carrier,
                List<TrackingItemDto> items);
    }
}
