using Microsoft.Extensions.Configuration;

namespace GestorInventario.Infrastructure.Configuration;

public class EnvironmentConfig
{
    private readonly IConfiguration _configuration;

    public EnvironmentConfig(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    #region Entorno y URLs Base

    /// <summary>Indica si la aplicación se está ejecutando dentro de un contenedor Docker.</summary>
    public bool IsDocker => string.Equals(Environment.GetEnvironmentVariable("IS_DOCKER"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>URL base principal de la aplicación (Dinámica según entorno Local o Docker).</summary>
    public string AppBaseUrl => GetOrThrow("APP_BASE_URL_LOCAL", "APP_DOCKER_URL", "App:BaseUrl", "App:DockerUrl");

    #endregion

    #region Base de Datos

    public string DbHost     => GetOrThrow("DB_HOST", "DatabaseSettings:DBHost");
    public string DbName     => GetOrThrow("DB_NAME", "DatabaseSettings:DBName");
    public string DbUserName => GetOrThrow("DB_SQLUSER", "DatabaseSettings:DBUserName");
    public string DbPassword => GetOrThrow("DB_SQLUSER_PASSWORD", "DatabaseSettings :DBPassword");

    #endregion

    #region Autenticación y JWT

    public string AuthMode   => GetOrThrow("AUTH_MODE", "AuthMode");
    public string Issuer     => GetOrThrow("JWT_ISSUER", "Jwt:Issuer");
    public string Audience   => GetOrThrow("JWT_AUDIENCE", "Jwt:Audience");
    public string ClaveJwt   => GetOrThrow("CLAVE_JWT", "JWT:ClaveJWT");
    public string PrivateKey => GetOrThrow("PRIVATE_KEY", "JWT:PrivateKey");
    public string PublicKey  => GetOrThrow("PUBLIC_KEY", "JWT:PublicKey");
    public string LoginMode  => GetOrThrow("LOGIN_MODE", "LoginMode");

    #endregion

    #region Pasarela de Pagos (PayPal)

    public string PaypalBaseUrl   => GetOrThrow("PAYPAL_BASEURL", "PayPal:BaseUrl");
    public string ClientId        => GetOrThrow("PAYPAL_CLIENTID", "Paypal:ClientId");
    public string ClientSecret    => GetOrThrow("PAYPAL_CLIENTSECRET", "Paypal:ClientSecret");
    public string PaypalReturnUrl => GetOrThrow("PAYPAL_RETURN_URL", "PayPal:ReturnUrls:Development", "PayPal:ReturnUrls:Docker");
    public string PaypalCancelUrl => GetOrThrow("PAYPAL_CANCEL_URL", "PayPal:CancelUrls:Development", "PayPal:CancelUrls:Docker");

    #endregion

    #region Caché (Redis)

    public string RedisConectionString => GetOrThrow("REDIS_CONNECTION_STRING", "Redis:ConnectionString");
    public bool   UseRedis             => GetBool("USE_REDIS", "UseRedis", defaultValue: false);

    #endregion

    #region Correo Electrónico (Email)

    public string EmailUser     => GetOrThrow("EMAIL_USERNAME", "Email:UserName");
    public string EmailPassword => GetOrThrow("EMAIL_PASSWORD", "Email:PassWord");
    public string EmailHost     => GetOrThrow("EMAIL_HOST", "Email:Host");
    public string EmailPort     => GetOrThrow("EMAIL_PORT", "Email:Port");

    #endregion

    #region Notificaciones (Telegram)

    public string TelegramUser => GetOrThrow("TELEGRAM_USER", "CallMeBot:TelegramUser");

    #endregion

    #region Métodos de Validación y Helpers Privados

    private string GetOrThrow(string envKey, string configKey)
    {
        var value = Environment.GetEnvironmentVariable(envKey) ?? _configuration[configKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"[CONFIG ERROR] Falta la variable obligatoria: {envKey} (o su equivalente en config).");
        }
        return value;
    }

    // Sobrecarga para claves que cambian entre Desarrollo y Docker
    private string GetOrThrow(string envKey, string devConfigKey, string dockerConfigKey)
    {
        var configKey = IsDocker ? dockerConfigKey : devConfigKey;
        return GetOrThrow(envKey, configKey);
    }

    // Sobrecarga por si se requiere priorizar una variable de entorno distinta para local vs docker en AppBaseUrl
    private string GetOrThrow(string localEnvKey, string dockerEnvKey, string devConfigKey, string dockerConfigKey)
    {
        var envKeyToUse = IsDocker ? dockerEnvKey : localEnvKey;
        var configKeyToUse = IsDocker ? dockerConfigKey : devConfigKey;

        var value = Environment.GetEnvironmentVariable(envKeyToUse) ?? _configuration[configKeyToUse];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"[CONFIG ERROR] Falta la variable obligatoria: {envKeyToUse} (o su configuración '{configKeyToUse}').");
        }
        return value;
    }

    private bool GetBool(string envKey, string configKey, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(envKey) ?? _configuration[configKey];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        return bool.TryParse(value, out var result) && result;
    }

    #endregion
}