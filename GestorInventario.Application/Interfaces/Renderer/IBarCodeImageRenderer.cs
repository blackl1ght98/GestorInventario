using GestorInventario.Domain.enums.Productos;

namespace GestorInventario.Application.Interfaces.Renderer.Barcode
{
    public interface IBarCodeImageRenderer
    {
        Task<byte[]> RenderAsync(string barcode, BarcodeType type);
    }
}
