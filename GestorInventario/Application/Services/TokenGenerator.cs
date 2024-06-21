using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Application.Services
{
    public class TokenGenerator: ITokenGenerator
    {
        private readonly GestorInventarioContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<TokenGenerator> _logger;

        public TokenGenerator(GestorInventarioContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, ILogger<TokenGenerator> logger)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _logger = logger;
        }
        public async Task<DTOLoginResponse> GenerarTokenSimetrico(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);
            var claims = new List<Claim>()
            {
                 new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                 new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                 new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
                

            };

            var clave = _configuration["ClaveJWT"];
            var claveKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
            var signinCredentials = new SigningCredentials(claveKey, SecurityAlgorithms.HmacSha256);
            var securityToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: signinCredentials);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);
            return new DTOLoginResponse()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }
        public async Task<DTOLoginResponse> GenerarTokenAsimetricoFijo(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                    new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                    new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
                  
                };
            // Carga la clave privada desde la configuración
            var privateKey = _configuration["Jwt:PrivateKey"];

            // Convierte la clave privada a formato RSA
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);

            // Crea las credenciales de firma con la clave privada
            var signinCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            var securityToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: signinCredentials);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new DTOLoginResponse()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }
        public async Task<DTOLoginResponse> GenerarTokenAsimetricoDinamico(Usuario credencialesUsuario)
        {
            var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

            var claims = new List<Claim>()
                {
                    new Claim(ClaimTypes.Email, credencialesUsuario.Email),
                    new Claim(ClaimTypes.Role, credencialesUsuario.IdRolNavigation.Nombre),
                    new Claim(ClaimTypes.NameIdentifier, credencialesUsuario.Id.ToString())
                    
                };

            // Genera un nuevo par de claves RSA
            var rsa = new RSACryptoServiceProvider(2048);
            var privateKey = rsa.ToXmlString(true);
            var publicKey = rsa.ToXmlString(false);

            //Generacion de la clave de cifrado
            var claveCifrado = GenerarClaveCifrado();
            var privateKeyCifrada = Cifrar(Encoding.UTF8.GetBytes(privateKey), claveCifrado);
            var publicKeyCifrada = Cifrar(Encoding.UTF8.GetBytes(publicKey), claveCifrado);
            string claveCifradoString = Convert.ToBase64String(claveCifrado);

            //// Guarda las claves en las cookies
            //_httpContextAccessor.HttpContext?.Response.Cookies.Append("PrivateKey", Convert.ToBase64String(privateKeyCifrada), new CookieOptions { HttpOnly = true, IsEssential = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = null });
            //_httpContextAccessor.HttpContext?.Response.Cookies.Append("PublicKey", Convert.ToBase64String(publicKeyCifrada), new CookieOptions { HttpOnly = true, IsEssential = true, Secure = true, SameSite = SameSiteMode.Strict, Expires = null });
            // Guarda las claves en la memoria del servidor
            _memoryCache.Set(credencialesUsuario.Id.ToString() + "PrivateKey", privateKeyCifrada);
            _memoryCache.Set(credencialesUsuario.Id.ToString() + "PublicKey", publicKeyCifrada);

            // Guarda la clave de cifrado en la memoria del servidor
            _memoryCache.Set(credencialesUsuario.Id.ToString(), claveCifrado);

            // Crea las credenciales de firma con la clave privada
            var signinCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);

            var securityToken = new JwtSecurityToken(
                issuer: _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(1),
                signingCredentials: signinCredentials);
            var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

            return new DTOLoginResponse()
            {
                Id = credencialesUsuario.Id,
                Token = tokenString,
                Rol = credencialesUsuario.IdRolNavigation.Nombre,
            };
        }
        public byte[] GenerarClaveCifrado()
        {
            try
            {
                // Crea un nuevo array de bytes con una longitud de 32 bytes (256 bits).
                // Este será el tamaño de la clave de cifrado que se va a generar.
                var claveCifrado = new byte[32]; // 256 bits para AES

                // Llena el array de bytes 'claveCifrado' con valores aleatorios.
                // Esto genera la clave de cifrado.
                RandomNumberGenerator.Fill(claveCifrado);

                // Devuelve la clave de cifrado.
                return claveCifrado;
            }
            catch (Exception ex)
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
                _logger.LogCritical(message: "Error al generar la clave de cifrado: ", ex);
                return new byte[0];
            }

        }

        public byte[] Cifrar(byte[] data, byte[] claveCifrado)
        {
            try
            {
                // Crea una nueva instancia de la clase AesManaged.
                using (var aes = Aes.Create())
                {
                    // Establece la clave de cifrado que se utilizará para el cifrado.
                    aes.Key = claveCifrado;

                    // Establece el modo de cifrado en CBC (Cipher Block Chaining).
                    aes.Mode = CipherMode.CBC;

                    // Establece el modo de relleno en PKCS7.
                    aes.Padding = PaddingMode.PKCS7;

                    // Genera un nuevo Vector de Inicialización (IV) aleatorio, esto es un valor aleatorio.
                    aes.GenerateIV();

                    // Crea un objeto de cifrado que se utiliza para transformar los datos.
                    using (var encryptor = aes.CreateEncryptor())
                    {
                        // Cifra los datos.
                        var cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);


                        // Esto es necesario porque el IV debe ser conocido para descifrar los datos más tarde,
                        // pero no necesita mantenerse en secreto.
                        return aes.IV.Concat(cipherText).ToArray(); // Prepend IV to the cipher text
                    }
                }
            }
            catch (Exception ex)
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
                _logger.LogCritical("Error al cifrar", ex);
                return new byte[0];
            }


        }
        public byte[] Descifrar(byte[] data, byte[] claveCifrado)
        {
            try
            {
                // Crea una nueva instancia de la clase AesManaged.
                // Esta clase proporciona una implementación del Algoritmo Estándar de Cifrado Avanzado (AES).
                using (var aes = Aes.Create())
                {
                    // Establece la clave de cifrado que se utilizará para el descifrado.
                    aes.Key = claveCifrado;

                    // Establece el modo de cifrado en CBC (Cipher Block Chaining).
                    aes.Mode = CipherMode.CBC;

                    // Establece el modo de relleno en PKCS7.
                    aes.Padding = PaddingMode.PKCS7;

                    // Extrae el Vector de Inicialización (IV) del texto cifrado.
                    // El IV se encuentra en los primeros bytes del texto cifrado y tiene una longitud igual a la longitud de bloque de AES (en bytes).
                    var iv = data.Take(aes.BlockSize / 8).ToArray(); // Extract IV from the cipher text

                    // Extrae el texto cifrado real, que comienza después del IV.
                    var cipherText = data.Skip(aes.BlockSize / 8).ToArray();

                    // Establece el IV que se utilizará para el descifrado.
                    aes.IV = iv;

                    // Crea un objeto de descifrado que se utiliza para transformar los datos.
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        // Descifra el texto cifrado y devuelve los datos originales.
                        return decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length);
                    }
                }

            }
            catch (Exception ex)
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
                return new byte[0];
            }

        }
    }
}
