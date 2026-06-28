using GestorInventario.enums.Pedido;
using GestorInventario.enums.Productos;
using GestorInventario.Shared.DTOS.Paypal.Responses.POST.Order;
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
            BarcodeType barcode,
            List<TrackingItemDto> items);
    }
}
