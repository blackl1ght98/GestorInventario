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

            /*Hacemos una llamada al servicio "RSACryptoServiceProvider" encargado de la gestion y configuracion de la claves rsa o clave
             asimetrica, el motivo por el que se poner "RSACryptoServiceProvider(2048)" es para decir que longitud va a tener nuestra clave
            rsa en este caso va a tener una longitud de 2048 bits que es la longitud estandar para claves rsa*/
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                /*Para el cifrado que vamos a realizar primero creamos las 2 claves asimetricas de manera dinamica 
                 1 clave privada "privateKey" y 1 clave publica "publicKey"*/
                var privateKey = rsa.ExportParameters(true);
                var publicKey = rsa.ExportParameters(false);
                /*Despues generamos de manera dinamica nuestra clave aes o clave simetrica, en la primera variable 
                 " var aes = Aes.Create()" esto crea el objeto criptográfico que almacenara la clave simetrica,
                para generar dicha clave ponemos esto " aes.GenerateKey();" y por ultimo guardamos en una variable
                el valor de esa clave "var aesKey = aes.Key;"*/
                var aes = Aes.Create();
                aes.GenerateKey();
                var aesKey = aes.Key;
                /*Una vez creadas ambas claves aes(simetrico) y rsa(asimetrico), lo primero que hacemos es hacer uso del valor
                 de la clave publica que hemos generado de esta manera "rsa.ImportParameters(publicKey);"*/
                rsa.ImportParameters(publicKey);
                /*Haciendo uso del valor de la clave publica rsa(asimetrica) ciframos nuestra clave aes(simetrica) haciendo uso del 
                 * metodo Encrypt de rsa, por lo tanto la clave aes queda cifrada con la clave publica rsa(asimetrica)*/
                var encryptedAesKey = rsa.Encrypt(aesKey, false);

              
                /*Aqui usamos nuestro metodo Cifrar que hemos creado el metodo que hemos echo el tipo de cifrado que hace es simetrico o aes,
                 pues aqui lo que se cifra es el Modulus de la clave publica con aes. ¿Que es Modulus en una clave publica asimetrica?
                Es un numero muy grande  este numero es el que aporta seguridad a la clave publica. Al cifrarlo agregamos mas seguridad.*/
                var publicKeyCifrada = Cifrar(publicKey.Modulus, aesKey); 
               

                // Guarda la clave AES cifrada y la clave pública cifrada en el caché en memoria
                _memoryCache.Set(credencialesUsuario.Id.ToString() + "PrivateKey", privateKey);
                _memoryCache.Set(credencialesUsuario.Id.ToString() + "EncryptedAesKey", encryptedAesKey);
                _memoryCache.Set(credencialesUsuario.Id.ToString() + "PublicKey", publicKeyCifrada);
                //Esto es como nuestro token es firmado
                var signinCredentials = new SigningCredentials(new RsaSecurityKey(privateKey), SecurityAlgorithms.RsaSha256);

                var securityToken = new JwtSecurityToken(
                    issuer: _configuration["JwtIssuer"],
                    audience: _configuration["JwtAudience"],
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
