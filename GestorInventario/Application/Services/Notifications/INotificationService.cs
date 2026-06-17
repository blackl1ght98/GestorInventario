namespace GestorInventario.Application.Services.Notifications
{
    public interface INotificationService
    {
        Task<bool> SendWhatsAppNotificationAsync(string message);
    }
}
