
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Notifications.SendNotification.Email
{
    public interface IRefundNotification
    {



        Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, IEnumerable<int> detalleIdsReembolsados, decimal montoReembolsado, string motivo);

    }
}
