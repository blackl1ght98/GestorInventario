using GestorInventario.Domain.Models;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Subscription;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Application.Services.Notification
{
    public interface IRembolsoNotification
    {

     
 
        Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo);
    
    }
}
