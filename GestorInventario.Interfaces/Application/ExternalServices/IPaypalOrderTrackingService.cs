using GestorInventario.enums.Pedido;
using GestorInventario.enums.Productos;
using GestorInventario.Shared.DTOS.Paypal.BD;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalOrderTrackingService
    {
        Task<OperationResult<(string TrackingNumber, string TrackingUrl)>>
        AddTrackingAsync(
            string payPalOrderId,
            string captureId,
            Carrier carrier,
        
            List<TrackingItemDto> items);
    }
}
