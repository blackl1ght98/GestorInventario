using GestorInventario.enums;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalOrderTrackingService
    {
        Task<OperationResult<(int pedidoId, string trackingNumber, string trackingURL, string carrier)>> SeguimientoPedido(int pedidoId, Carrier carrier, BarcodeType barcode);
    }
}
