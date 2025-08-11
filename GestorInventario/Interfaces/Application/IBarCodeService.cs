using GestorInventario.Application.DTOs.Barcode;
using GestorInventario.enums;

namespace GestorInventario.Interfaces.Application
{
    public interface IBarCodeService
    {
        Task<BarcodeResult> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage);
    }
}
