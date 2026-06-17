using GestorInventario.Application.DTOs.Barcode;
using GestorInventario.enums.Productos;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IBarCodeService
    {
        Task<BarcodeResultDto> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage);
    }
}
