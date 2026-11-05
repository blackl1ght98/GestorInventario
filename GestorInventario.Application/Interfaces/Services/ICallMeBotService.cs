namespace GestorInventario.Application.Interfaces.Services
{
    public interface ICallMeBotService
    {
        Task<bool> SendWhatsAppNotificationAsync(string message);
    }
}
