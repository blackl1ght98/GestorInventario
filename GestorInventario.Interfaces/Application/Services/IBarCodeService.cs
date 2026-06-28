using GestorInventario.enums.Productos;
using GestorInventario.Shared.DTOS.Barcode;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IBarCodeService
    {
        Task<BarcodeResultDto> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage);
    }
}
