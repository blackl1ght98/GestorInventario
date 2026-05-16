using GestorInventario.enums;

namespace GestorInventario.Interfaces.Application.ExternalServices
{
    public interface IPaypalOrderTrackingService
    {
        Task<string> SeguimientoPedido(int pedidoId, Carrier carrier, BarcodeType barcode);
    }
}
