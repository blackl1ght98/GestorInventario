using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application
{
    public interface IPdfService
    {
        Task<OperationResult<byte[]>> GenerarPDF();
        Task<OperationResult<byte[]>> DescargarProductoPDF();
    }
}
