using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;

using Microsoft.EntityFrameworkCore;

namespace GestorInventario.Infrestructure.Repositories.NotificacionRepository
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
       
        public IQueryable<Notificacion> ObtenerNotificaciones(int usuarioId)
        {
            var notificaciones = _context.Notificacions.Include(x => x.Usuario).Where(u=>u.UsuarioId==usuarioId).AsQueryable();
            return notificaciones;
        }
        public async Task<OperationResult<string>> MarcarNotificacionComoLeida(int id)
        {

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var notificacion = await _context.Notificacions.FindAsync(id);
                if (notificacion is null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                notificacion.Leida = true;

                await _context.UpdateEntityAsync(notificacion);

                return OperationResult<string>.Ok("Notificacion marcada con exito");

            });

        }
        public async Task<OperationResult<string>> MarcarTodasNotificacionesComoLeidas(int usuarioId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                // Una sola query SQL: UPDATE Notificacions SET Leida = 1 WHERE UsuarioId = @p0 AND Leida = 0
                int filasAfectadas = await _context.Notificacions
                    .Where(n => n.UsuarioId == usuarioId && !n.Leida)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(n => n.Leida, true));
                       

                return OperationResult<string>.Ok(
                    $"{filasAfectadas} notificación(es) marcada(s) como leídas.");
            });
        }
        public async Task<int> EliminarNotificacionesAntiguas(DateTime cutoff)
        {
            return await _context.Notificacions
                .Where(n => n.FechaCreacion < cutoff)
                .ExecuteDeleteAsync();
        }

    }
}

