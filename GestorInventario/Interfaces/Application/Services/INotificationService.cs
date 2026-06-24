using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface INotificationService
    {
        Task<OperationResult<string>> CrearNotificacion(int usuarioId, string titulo, string mensaje, string tipo);
    }
}
