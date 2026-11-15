using GestorInventario.Application.Interfaces.Services;
using GestorInventario.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace GestorInventario.Infrastructure.Services
{
    public class UrlService : IUrlService
    {
        private readonly string _baseUrl;
        private readonly string _paypalReturnUrl;
        private readonly string _paypalCancelUrl;

        public UrlService(IConfiguration configuration)
        {
            var envConfig = new EnvironmentConfig(configuration);

            // Validamos y estandarizamos las URLs obtenidas del configurador central
            _baseUrl = ValidateAbsoluteUrl("AppBaseUrl", envConfig.AppBaseUrl);
            _paypalReturnUrl = ValidateAbsoluteUrl("PaypalReturnUrl", envConfig.PaypalReturnUrl);
            _paypalCancelUrl = ValidateAbsoluteUrl("PaypalCancelUrl", envConfig.PaypalCancelUrl);
        }

        public string BuildUrl(string path)
            => string.IsNullOrWhiteSpace(path)
                ? _baseUrl
                : $"{_baseUrl}/{path.TrimStart('/')}";

        public string GetPaypalReturnUrl() => _paypalReturnUrl;
        public string GetPaypalCancelUrl() => _paypalCancelUrl;

        // ─── Validación ─────────────────────────────────────────

        private static string ValidateAbsoluteUrl(string fieldName, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException(
                    $"La configuración '{fieldName}' está vacía. " +
                    $"Revisa appsettings.json, secretos de usuario o variables de entorno.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    $"La configuración '{fieldName}'='{url}' no es una URL absoluta válida (http/https).");
            }

            return url.TrimEnd('/');
        }
    }
}