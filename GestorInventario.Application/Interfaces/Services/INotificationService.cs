using GestorInventario.Domain.enums.Notification;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Application.Interfaces.Services
{
    public interface INotificationService
    {
        Task<OperationResult<string>> CrearNotificacion( string titulo, string mensaje, TipoNotificacion tipo);
    }
}
