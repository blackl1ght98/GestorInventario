using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IReembolsoNotificationService
    {
        Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo);
    }
}