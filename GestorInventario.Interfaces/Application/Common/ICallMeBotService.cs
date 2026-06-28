namespace GestorInventario.Interfaces.Application.Common
{
    public interface ICallMeBotService
    {
        Task<bool> SendWhatsAppNotificationAsync(string message);
    }
}
