using GestorInventario.Application.DTOS.Barcode;
using GestorInventario.Domain.enums.Productos;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface IBarCodeService
    {
        Task<BarcodeResultDto> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage);
    }
}
