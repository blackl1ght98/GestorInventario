using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Common;
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
        public async Task<OperationResult<string>> CrearNotificacion(int usuarioId, string titulo, string mensaje, string tipo)
        {
            var notificacion = new Notificacion
            {
                UsuarioId=usuarioId,
                Titulo=titulo,
                Mensaje=mensaje,
                Tipo= tipo,
                Leida=false,
                FechaCreacion=DateTime.UtcNow
            };
            await _notificacion.CrearNotificacion(notificacion);
            return OperationResult<string>.Ok("Notificacion creada con exito");
        }
    }
}
