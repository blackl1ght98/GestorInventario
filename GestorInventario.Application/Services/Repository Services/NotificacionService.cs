
using GestorInventario.Domain.Models;

using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;
using GestorInventario.Utilities;
using System.ClientModel.Primitives;

namespace GestorInventario.Application.Services.Repository_Services
{
    public class NotificacionService: INotificationService
    {
        private readonly INotificationRepository _notificacion;
        private readonly ICurrentUserAccessor _current;

        public NotificacionService(INotificationRepository notificacion, ICurrentUserAccessor current)
        {
            _notificacion = notificacion;
            _current = current;
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
