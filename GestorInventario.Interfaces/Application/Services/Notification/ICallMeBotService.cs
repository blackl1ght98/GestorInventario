namespace GestorInventario.Interfaces.Application.Services.Notification
{
    public interface ICallMeBotService
    {
        Task<bool> SendWhatsAppNotificationAsync(string message);
    }
}
