
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Shared.DTOS.Configuration;
using Microsoft.Extensions.Options;

namespace GestorInventario.Application.Services.Common
{
    public class UrlService : IUrlService
    {
        private readonly AppSettings _appSettings;

        public UrlService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        public string GetBaseUrl()
        {
            // 1) Prioridad: variable de entorno explícita (override absoluto)
            var envOverride = Environment.GetEnvironmentVariable("APP_BASE_URL");
            if (!string.IsNullOrWhiteSpace(envOverride))
                return envOverride.TrimEnd('/');

            // 2) Detección automática de Docker
            if (IsRunningInDocker())
            {
                // Override específico de Docker (case-insensitive por si acaso)
                var dockerOverride = Environment.GetEnvironmentVariable("APP_DOCKER_URL");
                if (!string.IsNullOrWhiteSpace(dockerOverride))
                    return dockerOverride.TrimEnd('/');

                return FirstNonEmpty(_appSettings.DockerUrl, _appSettings.BaseUrl);
            }

            // 3) Fallback al base local
            var baseOverride = Environment.GetEnvironmentVariable("APP_BASE_URL_LOCAL");
            if (!string.IsNullOrWhiteSpace(baseOverride))
                return baseOverride.TrimEnd('/');

            return FirstNonEmpty(_appSettings.BaseUrl, _appSettings.DockerUrl);
        }

        public string BuildUrl(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return GetBaseUrl();

            // path esperado tipo "/Pedidos/DownloadInvoice?id=123"
            return $"{GetBaseUrl()}{path}";
        }

        private static bool IsRunningInDocker()
        {
           
            if (string.Equals(
                    Environment.GetEnvironmentVariable("IS_DOCKER"),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
                return true;

         
            return File.Exists("/.env");
        }

        private static string FirstNonEmpty(params string[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.TrimEnd('/')
               ?? throw new InvalidOperationException("No se ha configurado ninguna URL base en AppSettings.");
    }
}
