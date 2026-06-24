using GestorInventario.Application.DTOS.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.MetodosExtension;

namespace GestorInventario.Infraestructure.Repositories.NotificacionRepository
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly GestorInventarioContext _context;

        public NotificationRepository(GestorInventarioContext context)
        {
            _context = context;
        }
        public async Task<OperationResult<Notificacion>> CrearNotificacion(Notificacion notificacion)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.AddEntityAsync(notificacion);
                return OperationResult<Notificacion>.Ok("Notificacion agregada con exito");



            });
        }
    }
}
