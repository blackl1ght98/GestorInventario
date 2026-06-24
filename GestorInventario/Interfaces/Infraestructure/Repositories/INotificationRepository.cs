using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Infraestructure.Repositories
{
    public interface INotificationRepository
    {
        Task<OperationResult<Notificacion>> CrearNotificacion(Notificacion notificacion);
    }
}
