using GestorInventario.enums.Productos;


namespace GestorInventario.Interfaces.Application.Services
{
    public interface IBarCodeImageRenderer
    {
        Task<byte[]> RenderAsync(string barcode, BarcodeType type);
    }
}
