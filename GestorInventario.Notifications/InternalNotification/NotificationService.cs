using GestorInventario.Domain.enums.Notification;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Notifications.InternalNotification;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Notifications.InternalNotification
{
    public class NotificationService: INotificationService
    {
        private readonly INotificationRepository _notificacion;
       

        public NotificationService(INotificationRepository notificacion)
        {
            _notificacion = notificacion;
            
        }
        //PULIR SISTEMA DE NOTIFICACIONES INTERNAS
        public async Task<OperationResult<string>> CrearNotificacion(int usuarioId, string titulo, string mensaje, TipoNotificacion tipo)
        {
            var notificacion = new Notificacion
            {
                UsuarioId=usuarioId,
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
