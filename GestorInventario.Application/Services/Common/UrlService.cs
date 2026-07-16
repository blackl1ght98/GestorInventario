
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Shared.DTOS.Configuration;
using Microsoft.Extensions.Options;

namespace GestorInventario.Application.Services.Common
{
    public class UrlService : IUrlService
    {
        private readonly string _baseUrl;
        private readonly string _paypalReturnUrl;
        private readonly string _paypalCancelUrl;

        public UrlService(
            IOptions<AppSettings> appSettings,
            IOptions<PaypalSettings> paypalSettings)
        {
            _baseUrl = ResolveBaseUrl(appSettings.Value);
            _paypalReturnUrl = ResolvePaypalReturnUrl(paypalSettings.Value);
            _paypalCancelUrl = ResolvePaypalCancelUrl(paypalSettings.Value);
        }

        public string BuildUrl(string path)
            => string.IsNullOrWhiteSpace(path)
                ? _baseUrl
                : $"{_baseUrl}{path}";

        public string GetPaypalReturnUrl()=> _paypalReturnUrl;
        public string GetPaypalCancelUrl() => _paypalCancelUrl;

        // ─── Resolución de URLs ────────────────────────────────

        private static string ResolveBaseUrl(AppSettings settings)
        {
            var envBase = Environment.GetEnvironmentVariable("APP_BASE_URL");
            if (!string.IsNullOrWhiteSpace(envBase))
                return ValidateAbsoluteUrl("AppSettings.BaseUrl", envBase);

            var candidate = IsRunningInDocker()
                ? FirstNonEmpty(
                    Environment.GetEnvironmentVariable("APP_DOCKER_URL"),
                    settings.DockerUrl)
                : FirstNonEmpty(
                    Environment.GetEnvironmentVariable("APP_BASE_URL_LOCAL"),
                    settings.BaseUrl);

            return ValidateAbsoluteUrl(
                IsRunningInDocker() ? "AppSettings.DockerUrl" : "AppSettings.BaseUrl",
                candidate);
        }

        private static string ResolvePaypalReturnUrl(PaypalSettings settings)
        {
            var envOverride = Environment.GetEnvironmentVariable("PAYPAL_RETURN_URL");
            if (!string.IsNullOrWhiteSpace(envOverride))
                return ValidateAbsoluteUrl("PaypalSettings.ReturnUrls", envOverride);

            var url = IsRunningInDocker()
                ? settings.ReturnUrls.Docker
                : settings.ReturnUrls.Development;

            var fieldName = IsRunningInDocker()
                ? "PaypalSettings.ReturnUrls.Docker"
                : "PaypalSettings.ReturnUrls.Development";

            return ValidateAbsoluteUrl(fieldName, url);
        }

        private static string ResolvePaypalCancelUrl(PaypalSettings settings)
        {
            var envOverride = Environment.GetEnvironmentVariable("PAYPAL_CANCEL_URL");
            if (!string.IsNullOrWhiteSpace(envOverride))
                return ValidateAbsoluteUrl("PaypalSettings.CancelUrls", envOverride);

            var url = IsRunningInDocker()
                ? settings.CancelUrls.Docker
                : settings.CancelUrls.Development;

            var fieldName = IsRunningInDocker()
                ? "PaypalSettings.CancelUrls.Docker"
                : "PaypalSettings.CancelUrls.Development";

            return ValidateAbsoluteUrl(fieldName, url);
        }

        // ─── Helpers ───────────────────────────────────────────

        private static bool IsRunningInDocker()
            => string.Equals(
                Environment.GetEnvironmentVariable("IS_DOCKER"),
                "true",
                StringComparison.OrdinalIgnoreCase);

        private static string FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
               ?? throw new InvalidOperationException(
                   "No se ha configurado ninguna URL base en AppSettings.");

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
