using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.Interfaces.Infraestructure.Repositories
{
    public interface INotificationRepository
    {
        Task<OperationResult<Notificacion>> CrearNotificacion(Notificacion notificacion);
        IQueryable<Notificacion> ObtenerNotificaciones(int usuarioId);
        Task<OperationResult<string>> MarcarNotificacionComoLeida(int id);
        Task<OperationResult<string>> MarcarTodasNotificacionesComoLeidas(int usuarioId);
        Task<int> EliminarNotificacionesAntiguas(DateTime cutoff);
    }
}
