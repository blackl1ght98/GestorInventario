using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application
{
    public interface IPaymentService
    {
        Task<OperationResult<string>> Pagar(string moneda, int userId);
    }
}
