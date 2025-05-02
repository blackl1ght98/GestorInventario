using GestorInventario.Interfaces.Application;
using System.Security.Cryptography;

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

        //// Método para cifrar
        public byte[] Cifrar(byte[] data, byte[] aesKey)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.GenerateIV();
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        var cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);
                        return aes.IV.Concat(cipherText).ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Error al cifrar", ex);
                throw;
            }
        }

        // Método para descifrar la clave AES utilizando la clave privada
        public byte[] Descifrar(byte[] encryptedData, RSAParameters privateKeyParams)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.ImportParameters(privateKeyParams);
                    return rsa.Decrypt(encryptedData, true);
                }
            }
            catch (Exception ex)
            {
                HandleDecryptionError(ex);
                return new byte[0];
            }
        }



        // Método para descifrar datos utilizando la clave AES
        public byte[] Descifrar(byte[] data, byte[] aesKey)
        {
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = aesKey;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    var iv = data.Take(aes.BlockSize / 8).ToArray();
                    var cipherText = data.Skip(aes.BlockSize / 8).ToArray();
                    aes.IV = iv;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDecryptionError(ex);
                return new byte[0];
            }
        }

        // Método para manejar errores de descifrado
        public void HandleDecryptionError(Exception ex)
        {
            var collectioncookies = _httpContextAccessor.HttpContext?.Request.Cookies;
            foreach (var cookie in collectioncookies!)
            {
                _httpContextAccessor.HttpContext?.Response.Cookies.Delete(cookie.Key);
            }
            if (_httpContextAccessor.HttpContext?.Request.Path != "/Auth/Login")
            {
                _httpContextAccessor.HttpContext?.Response.Redirect("/Auth/Login");
            }
            _logger.LogCritical("Error al descifrar", ex);
        }
    }
}

