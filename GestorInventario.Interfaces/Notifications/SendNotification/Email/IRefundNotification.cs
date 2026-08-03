
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Notifications.SendNotification.Email
{
    public interface IRefundNotification
    {

     
 
        Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo);
    
    }
}
