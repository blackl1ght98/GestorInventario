using GestorInventario.Interfaces.Application.Services.Notification;
using Microsoft.Extensions.Configuration;

namespace GestorInventario.Notifications.Mensajes.Telegram
{
    public class CallMeBotService : ICallMeBotService
    {
        private readonly HttpClient _httpClient;
        private readonly string _username;

        public CallMeBotService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Obtenemos el usuario desde el appsettings.json
            _username = configuration["CallMeBot:TelegramUser"] ?? Environment.GetEnvironmentVariable("TELEGRAM_USER");
        }

        public async Task<bool> SendWhatsAppNotificationAsync(string message)
        {
            if (string.IsNullOrEmpty(_username))
            {
                throw new Exception("El usuario de CallMeBot no está configurado en appsettings.json");
            }

            // IMPORTANTE: El texto debe estar codificado para URLs (URL Encoding)
            // Esto evita que espacios o caracteres especiales rompan la petición
            var encodedMessage = Uri.EscapeDataString(message);

            var url = $"https://api.callmebot.com/text.php?user={_username}&text={encodedMessage}";

            try
            {
                // Hacemos la petición GET
                var response = await _httpClient.GetAsync(url);

                // Retornamos true si la respuesta fue exitosa (200-299)
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                // Loggear el error aquí
                Console.WriteLine($"Error enviando notificación: {ex.Message}");
                return false;
            }
        }
    }
}
