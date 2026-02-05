using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Application.Services.Authentication
{
    public class EncryptionService : IEncryptionService
    {
        private readonly ILogger<EncryptionService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
       

        public EncryptionService(ILogger<EncryptionService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        // Método existente: descifra la clave AES con la privada RSA
        public byte[] Descifrar(byte[] encryptedData, byte[] privateKeyBytes)
        {
            try
            {
                using var rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                return rsa.Decrypt(encryptedData, RSAEncryptionPadding.OaepSHA256);
            }
            catch (Exception ex)
            {
                HandleDecryptionError(ex);
                return Array.Empty<byte>();
            }
        }

        // Nuevo método: descifra la clave privada RSA cifrada con la master key
        public string? DescifrarClavePrivada(string? encryptedBase64)
        {
            if (string.IsNullOrEmpty(encryptedBase64))
            {
                _logger.LogWarning("Intento de descifrar clave privada vacía");
                return null;
            }

            try
            {
                // ¡Aquí está el punto crítico!
                // Necesitas la misma MasterKey que usaste al cifrar
                // Opción temporal: acceder directamente si la hiciste pública en la otra clase
                byte[] masterKey = AsymmetricDynamicTokenStrategy.MasterKey; // ← Solo si la declaraste public static

                // Alternativa mejor: usar un helper estático compartido (CryptoHelper)
                // byte[] masterKey = CryptoHelper.MasterKey;

                byte[] encryptedData = Convert.FromBase64String(encryptedBase64);

                if (encryptedData.Length < 16)
                {
                    _logger.LogWarning("Datos cifrados de clave privada demasiado cortos");
                    return null;
                }

                byte[] iv = encryptedData.Take(16).ToArray();
                byte[] cipherText = encryptedData.Skip(16).ToArray();

                using var aes = Aes.Create();
                aes.Key = masterKey;
                aes.IV = iv;

                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(cipherText, 0, cipherText.Length);
                cs.FlushFinalBlock();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descifrar la clave privada RSA cifrada");
                HandleDecryptionError(ex);
                return null;
            }
        }
       
        public void HandleDecryptionError(Exception ex)
        {
            var collectioncookies = _httpContextAccessor.HttpContext?.Request.Cookies;
            if (collectioncookies != null)
            {
                foreach (var cookie in collectioncookies)
                {
                    _httpContextAccessor.HttpContext?.Response.Cookies.Delete(cookie.Key);
                }
            }

            if (_httpContextAccessor.HttpContext?.Request.Path != "/Auth/Login")
            {
                _httpContextAccessor.HttpContext?.Response.Redirect("/Auth/Login");
            }

            _logger.LogCritical(ex, "Error al descifrar");
        }
    }
}