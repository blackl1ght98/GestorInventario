using GestorInventario.Application.DTOS.Barcode;
using GestorInventario.Domain.enums.Productos;

namespace GestorInventario.Application.Interfaces.Application.Services.Common
{
    public interface IBarCodeService
    {
        Task<BarcodeResultDto> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage);
    }
}
