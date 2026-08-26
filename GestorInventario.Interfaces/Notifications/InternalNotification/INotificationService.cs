using GestorInventario.Domain.enums.Notification;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Notifications.InternalNotification
{
    public interface INotificationService
    {
        Task<OperationResult<string>> CrearNotificacion(int usuarioId, string titulo, string mensaje, TipoNotificacion tipo);
    }
}
