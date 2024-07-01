using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Middlewares
{
    public static class MidlewareAutenticacionAsimetricaDinamica
    {
        public static IApplicationBuilder MiddlewareAutenticacionAsimetricaDinamica(this IApplicationBuilder app, WebApplicationBuilder builder)
        {


            //MIDDLEWARE PARA MANEJO ASIMETRICO CON CLAVES DINAMICAS MULTIUSUARIO
            //app.Use(async (context, next) =>
            //{
            //    try
            //    {
            //        var collectioncookies = context.Request.Cookies;
            //        var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
            //        var tokenservice = context.RequestServices.GetRequiredService<ITokenGenerator>();
            //        var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();

            //        var token = context.Request.Cookies["auth"];
            //        // Si el token existe...
            //        if (token != null)
            //        {
            //            // Crea un nuevo manejador de tokens JWT.
            //            var handler = new JwtSecurityTokenHandler();
            //            // Carga la clave pública cifrada desde las cookies
            //            // var publicKeyCifrada = httpContextAccessor.HttpContext.Request.Cookies["PublicKey"];
            //            // Carga la clave pública cifrada desde la memoria del servidor
            //            var userId2 = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

            //            memoryCache.TryGetValue(userId2 + "PublicKey", out byte[] publicKeyCifrada);

            //            if (publicKeyCifrada == null)
            //            {
            //                foreach (var cookie in collectioncookies)
            //                {
            //                    context.Response.Cookies.Delete(cookie.Key);
            //                }
            //                if (context.Request.Path != "/Auth/Login")
            //                {
            //                    context.Response.Redirect("/Auth/Login");
            //                }
            //            }
            //            else
            //            {
            //                // Obtiene la clave de cifrado del usuario
            //                var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            //                memoryCache.TryGetValue(userId, out byte[] claveCifrado);
            //                if (claveCifrado == null)
            //                {
            //                    foreach (var cookie in collectioncookies)
            //                    {
            //                        context.Response.Cookies.Delete(cookie.Key);
            //                    }
            //                    if (context.Request.Path != "/Auth/Login")
            //                    {
            //                        context.Response.Redirect("/Auth/Login");
            //                    }
            //                }
            //                else
            //                {
            //                    var publicKey = Encoding.UTF8.GetString(tokenservice.Descifrar(publicKeyCifrada, claveCifrado));
            //                    // Convierte la clave pública a formato RSA
            //                    var rsa = new RSACryptoServiceProvider();
            //                    rsa.FromXmlString(publicKey);

            //                    // Valida el token.
            //                    var principal = handler.ValidateToken(token, new TokenValidationParameters
            //                    {
            //                        ValidateIssuerSigningKey = true,
            //                        // Establece la clave que se debe usar para validar la firma del token.
            //                        IssuerSigningKey = new RsaSecurityKey(rsa),
            //                        ValidateIssuer = true,
            //                        ValidIssuer = builder.Configuration["JwtIssuer"],
            //                        ValidateAudience = true,
            //                        ValidAudience = builder.Configuration["JwtAudience"],
            //                    }, out var validatedToken);

            //                    // Establece el usuario del contexto actual a partir de la información del token.
            //                    context.User = principal;
            //                    token = context.Session.GetString("auth") ?? context.Request.Cookies["auth"];
            //                    context.Session.SetString("auth", token);
            //                }
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        var collectioncookies = context.Request.Cookies;

            //        foreach (var cookie in collectioncookies)
            //        {
            //            context.Response.Cookies.Delete(cookie.Key);
            //        }
            //        if (context.Request.Path != "/Auth/Login")
            //        {
            //            context.Response.Redirect("/Auth/Login");
            //        }
            //        var logger = log4net.LogManager.GetLogger(typeof(Program));
            //        logger.Error("Error con las claves", ex);
            //    }

            //    // Pasa el control al siguiente middleware en la cadena.
            //    await next.Invoke();
            //});
            app.Use(async (context, next) =>
            {
                try
                {
                    //Esta es la manera correcta de llamar a servicios en el program cuando requerimos hacer uso de ellos
                    var collectioncookies = context.Request.Cookies;
                    var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                    var tokenservice = context.RequestServices.GetRequiredService<ITokenGenerator>();
                    var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();
                    //Obtenemos el token de las cookies
                    var token = context.Request.Cookies["auth"];
                    // Si el token existe...
                    if (token != null)
                    {
                        // Crea un nuevo manejador de tokens JWT.
                        var handler = new JwtSecurityTokenHandler();
                       //Obtenemos el id del usuario de manera automatica debido a que esta en las cookies
                        var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                        //Obtenemos la clave aes cifrada y la clave privada de la memoria del servidor
                        memoryCache.TryGetValue(userId + "EncryptedAesKey", out byte[] encryptedAesKey);
                        memoryCache.TryGetValue(userId + "PrivateKey", out RSAParameters? privateKey);
                        //Si uno de los valores anteriores almacenados en memoria es null borramos las cookies esto se hace
                        //porque si un usuario no ha cerrado sesion pues se le borran las cookies
                        if (encryptedAesKey == null || privateKey == null)
                        {
                            foreach (var cookie in collectioncookies)
                            {
                                context.Response.Cookies.Delete(cookie.Key);
                            }
                            if (context.Request.Path != "/Auth/Login")
                            {
                                context.Response.Redirect("/Auth/Login");
                            }
                        }
                        else
                        {
                            /*Llegados a este punto ambos valores esta en memoria del servidor
                             Aqui lo que hacemos es decifrar la clave aes con la clave privada rsa
                             */
                            
                            var aesKey = tokenservice.Descifrar(encryptedAesKey, privateKey.Value);
                             //Obtenemos la clave publica de la memoria
                            memoryCache.TryGetValue(userId + "PublicKey", out byte[] publicKeyCifrada);
                            //Si la clave publica no existe...
                            if (publicKeyCifrada == null)
                            {
                                //Borramos las cookies
                                foreach (var cookie in collectioncookies)
                                {
                                    context.Response.Cookies.Delete(cookie.Key);
                                }
                                if (context.Request.Path != "/Auth/Login")
                                {
                                    context.Response.Redirect("/Auth/Login");
                                }
                            }
                            else
                            {
                                /*llegados a este punto la clave publica existe y esta almacenada por lo tando usando la clave
                                 aes desciframos la clave publica*/
                                var publicKey = tokenservice.Descifrar(publicKeyCifrada, aesKey);

                                // Valida el token.
                                var principal = handler.ValidateToken(token, new TokenValidationParameters
                                {
                                    ValidateIssuerSigningKey = true,
                                    // Establece la clave que se debe usar para validar la firma del token.
                                    IssuerSigningKey = new RsaSecurityKey(RSA.Create(new RSAParameters
                                    {
                                        //Recodemos aqui que el modulo hemos dicho que es un numero muy grande es el que aporta seguridad
                                        //es una parte de la clave publica
                                        //El Modulus es una parte importante de la clave publica
                                        Modulus = publicKey,
                                        //El exponente es otra parte de la clave publica, este valor se usa para cifrar y descifrar
                                        //0x010001-->0x no es parte del numero es una convecion que lo que este a partir de la x es hexadecimal
                                        //65537--> para obneter new byte[] { 1, 0, 1 }  primero esta numero 65537 se convierte a hexadecimal
                                        //en hexadecimal este numero 65537 es 10001 como hay que hacer grupos de bytes se coge de 2 en 2
                                        //01-00-01-->para realiza la agrupacion se realiza derecha a izquierza
                                        //Una vez echa la grupacion lo convertimos a decimal
                                        //01-->1
                                        //00-->0
                                        //01-->1
                                        //Una vez obtenidos los decimales los numeros se colocan en el orden en el que se leen (big endian)
                                        //El middleware averigua el numero asi: de los numeros decimales lo transforma a hexadecimal,
                                        //quita la agrupacion echa y obtiene el numero 65537
                                        // new byte[] { 1, 0, 1 } es una representacion en bytes del numero 65537
                                        Exponent = new byte[] { 1, 0, 1 }
                                        
                                    })),
                                    ValidateIssuer = true,
                                    ValidIssuer = builder.Configuration["JwtIssuer"],
                                    ValidateAudience = true,
                                    ValidAudience = builder.Configuration["JwtAudience"],
                                }, out var validatedToken);

                                // Establece el usuario del contexto actual a partir de la información del token.
                                context.User = principal;
                                token = context.Session.GetString("auth") ?? context.Request.Cookies["auth"];
                                context.Session.SetString("auth", token);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var collectioncookies = context.Request.Cookies;

                    foreach (var cookie in collectioncookies)
                    {
                        context.Response.Cookies.Delete(cookie.Key);
                    }
                    if (context.Request.Path != "/Auth/Login")
                    {
                        context.Response.Redirect("/Auth/Login");
                    }
                    var logger = log4net.LogManager.GetLogger(typeof(Program));
                    logger.Error("Error con las claves", ex);
                }

                await next.Invoke();
            });

         
            return app;
        }
    }
}
