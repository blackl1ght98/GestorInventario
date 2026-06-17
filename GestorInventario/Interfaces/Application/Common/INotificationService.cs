namespace GestorInventario.Interfaces.Application.Common
{
    public interface INotificationService
    {
        Task<bool> SendWhatsAppNotificationAsync(string message);
    }
}
