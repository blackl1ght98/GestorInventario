namespace GestorInventario.Application.Interfaces.Notifications.SendNotification.Telegram
{
    public interface ICallMeBotService
    {
        Task<bool> SendWhatsAppNotificationAsync(string message);
    }
}
