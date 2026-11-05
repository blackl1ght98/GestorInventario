using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
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
