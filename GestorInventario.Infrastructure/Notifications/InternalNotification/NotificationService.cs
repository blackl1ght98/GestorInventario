using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Notifications.InternalNotification;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Domain.enums.Notification;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Infrastructure.Notifications.InternalNotification
{
    public class NotificationService: INotificationService
    {
        private readonly INotificationRepository _notificacion;
        private readonly ICurrentUserAccessor  _currentUserAccessor;

        public NotificationService(INotificationRepository notificacion,ICurrentUserAccessor  currentUserAccessor)
        {
            _notificacion = notificacion;
            _currentUserAccessor = currentUserAccessor;
            
        }
        
        public async Task<OperationResult<string>> CrearNotificacion(string titulo, string mensaje, TipoNotificacion tipo)
        {
            
            var notificacion = new Notificacion
            {
                UsuarioId= _currentUserAccessor.GetCurrentUserId(),
                Titulo=titulo,
                Mensaje=mensaje,
                Tipo= tipo.ToString(),
                Leida=false,
                FechaCreacion=DateTime.UtcNow
            };
            await _notificacion.CrearNotificacion(notificacion);
            return OperationResult<string>.Ok("Notificacion creada con exito");
        }
    }
}
