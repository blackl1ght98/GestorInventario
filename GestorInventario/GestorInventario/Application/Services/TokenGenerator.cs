using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
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
        private readonly IDistributedCache _redis;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        public TokenGenerator(GestorInventarioContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor,
            IMemoryCache memoryCache, ILogger<TokenGenerator> logger, IDistributedCache cache, IConnectionMultiplexer connection)
        {
            _context = context;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
            _logger = logger;
            _redis = cache;
            _connectionMultiplexer=connection;
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

            var clave = Environment.GetEnvironmentVariable("ClaveJWT") ?? _configuration["ClaveJWT"];
            var claveKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave));
            var signinCredentials = new SigningCredentials(claveKey, SecurityAlgorithms.HmacSha256);
            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: Environment.GetEnvironmentVariable("JwtAudience") ?? _configuration["JwtAudience"],
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

            var privateKey = Environment.GetEnvironmentVariable("PrivateKey") ?? _configuration["JWT:PrivateKey"];

            // Convierte la clave privada a formato RSA
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);

            // Crea las credenciales de firma con la clave privada
            var signinCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
            var securityToken = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                audience: _configuration["JwtAudience"]??Environment.GetEnvironmentVariable("JwtAudience"),
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

            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                var privateKey = rsa.ExportParameters(true);
                var publicKey = rsa.ExportParameters(false);

                var aes = Aes.Create();
                aes.GenerateKey();
                var aesKey = aes.Key;

                rsa.ImportParameters(publicKey);
                var encryptedAesKey = rsa.Encrypt(aesKey, false);

                var publicKeyCifrada = Cifrar(publicKey.Modulus, aesKey);

                // Convertir las claves a base64 para almacenar
                var privateKeyJson = JsonConvert.SerializeObject(privateKey);
                var encryptedAesKeyBase64 = Convert.ToBase64String(encryptedAesKey);
                var publicKeyCifradaBase64 = Convert.ToBase64String(publicKeyCifrada);

                bool useRedis = _connectionMultiplexer != null && _connectionMultiplexer.GetDatabase().Ping().Milliseconds >= 0;

                if (useRedis)
                {
                    // Guarda la clave AES cifrada y la clave pública cifrada en Redis
                    await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PrivateKey", privateKeyJson);
                    await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "EncryptedAesKey", encryptedAesKeyBase64);
                    await _redis.SetStringAsync(credencialesUsuario.Id.ToString() + "PublicKey", publicKeyCifradaBase64);
                }
                else
                {
                    // Guarda la clave AES cifrada y la clave pública cifrada en memoria
                    _memoryCache.Set(credencialesUsuario.Id.ToString() + "PrivateKey", privateKeyJson);
                    _memoryCache.Set(credencialesUsuario.Id.ToString() + "EncryptedAesKey", encryptedAesKeyBase64);
                    _memoryCache.Set(credencialesUsuario.Id.ToString() + "PublicKey", publicKeyCifradaBase64);
                }

                var signinCredentials = new SigningCredentials(new RsaSecurityKey(privateKey), SecurityAlgorithms.RsaSha256);

                var securityToken = new JwtSecurityToken(
                    issuer: Environment.GetEnvironmentVariable("JwtIssuer") ?? _configuration["JwtIssuer"],
                    audience: _configuration["JwtAudience"]??Environment.GetEnvironmentVariable("JwtAudience"),
                    claims: claims,
                    expires: DateTime.Now.AddHours(24),
                    signingCredentials: signinCredentials);
                var tokenString = new JwtSecurityTokenHandler().WriteToken(securityToken);

                return new DTOLoginResponse()
                {
                    Id = credencialesUsuario.Id,
                    Token = tokenString,
                    Rol = credencialesUsuario.IdRolNavigation.Nombre,
                };
            }
        }


        /*Nuestro metodo cifrar devuelve un array de bytes este metodo se compone de lo que se va a cifrar que es el "data" y con que se va a 
         cifrar.*/
        public byte[] Cifrar(byte[] data, byte[] aesKey)
        {
            try
            {
                /*En este caso usamos aes para cifrar, la manera de inicializarlo es asi "var aes = Aes.Create()" pero primero hay que poner
                 un using.*/
                using (var aes = Aes.Create())
                {
                    //Le pasamo la llave con la que va a cifrar
                    aes.Key = aesKey;
                    //El modo de cifrado en este caso es CBC esto es que va tomando parte a parte de los datos y los va cifrando
                    aes.Mode = CipherMode.CBC;
                    //Cuando llega al final y no tiene datos suficientes ha cifrar usa este metodo de relleno
                    aes.Padding = PaddingMode.PKCS7;
                    //Generamos un vector de inicializacion que es un valor aleatorio
                    aes.GenerateIV();
                    /*Esto es lo que realmente cifra los datos, aqui creamos el objeto encargado de cifrar "var encryptor = aes.CreateEncryptor()"
                     esta variable tiene que ir dentro de un using
                     */
                    using (var encryptor = aes.CreateEncryptor())
                    {
                       
                        /*El metodo TransformFinalBlock dentro de encryptor recibe 3 parametros el primero son los datos, despues se dice 
                         como se inicializa el array y se obtiene la longitud de los datos y se cifra*/
                        var cipherText = encryptor.TransformFinalBlock(data, 0, data.Length);
                        //Concatena el vector de inicializacion con el valor cifrado y lo convierte a un array
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
    
    /*Aqui tenemos nuestro metodo descrifrar que devuelve un array de bytes y recibe 2 parametros el primero la informacion cifrada
     y el segundo unos valores especiales de la clave privada*/
    public byte[] Descifrar(byte[] encryptedData, RSAParameters privateKeyParams)
        {
            try
            {
                //Llamamos al servicio que ha encriptado nuestros datos
                using (var rsa = new RSACryptoServiceProvider())
                {
                    //Extrae el valor de la clave privada
                    rsa.ImportParameters(privateKeyParams);
                    //Haciendo uso de la clave privada descifra los datos
                    var decryptedAesKey = rsa.Decrypt(encryptedData, false);
                    //retorna los datos descifrados
                    return decryptedAesKey;
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
/*Aqui tenemos otro metodo descifrar pero este descifra la clave asimetrica recibe los datos y la clave*/
        public byte[] Descifrar(byte[] data, byte[] aesKey)
        {
            try
            {
                // Descifra los datos con la clave AES
                using (var aes = Aes.Create())
                {
                    //Se pasa la clave aes
                    aes.Key = aesKey;
                    //Se dice con que metodo se cifro
                    aes.Mode = CipherMode.CBC;
                    //Que relleno se uso para el cifrado
                    aes.Padding = PaddingMode.PKCS7;
                    //De los datos cifrados primero obtine el valor de la clave lo divide entre 8 y lo convierte a un array
                    var iv = data.Take(aes.BlockSize / 8).ToArray();
                    //Aqui salta a los siguientes datos cifrados y hace igual que el anterior
                    var cipherText = data.Skip(aes.BlockSize / 8).ToArray();
                    //Se facilita el vector de inicializacion usado
                    aes.IV = iv;
                    //Esto es lo que realmente descifra los datos
                    using (var decryptor = aes.CreateDecryptor())
                    {
                        //se pasa el valor cifrado como se inicializo y se obtiene la longitud del valor por ultimo se descifra
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
