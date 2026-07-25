namespace GestorInventario.Interfaces.Notifications.SendNotification.Telegram
{
    public interface ICallMeBotService
    {
        Task<bool> SendWhatsAppNotificationAsync(string message);
    }
}
