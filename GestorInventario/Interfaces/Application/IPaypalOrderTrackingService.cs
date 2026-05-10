using GestorInventario.enums;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaypalOrderTrackingService
    {
        Task<string> SeguimientoPedido(int pedidoId, Carrier carrier, BarcodeType barcode);
    }
}
