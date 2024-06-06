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
            app.Use(async (context, next) =>
            {
                try
                {
                    var collectioncookies = context.Request.Cookies;
                    var httpContextAccessor = context.RequestServices.GetRequiredService<IHttpContextAccessor>();
                    var tokenservice = context.RequestServices.GetRequiredService<ITokenGenerator>();
                    var memoryCache = context.RequestServices.GetRequiredService<IMemoryCache>();

                    var token = context.Request.Cookies["auth"];
                    // Si el token existe...
                    if (token != null)
                    {
                        // Crea un nuevo manejador de tokens JWT.
                        var handler = new JwtSecurityTokenHandler();
                        // Carga la clave pública cifrada desde las cookies
                        var publicKeyCifrada = httpContextAccessor.HttpContext.Request.Cookies["PublicKey"];
                        if (publicKeyCifrada == null)
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
                            // Obtiene la clave de cifrado del usuario
                            var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                            memoryCache.TryGetValue(userId, out byte[] claveCifrado);
                            if (claveCifrado == null)
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
                                var publicKey = Encoding.UTF8.GetString(tokenservice.Descifrar(Convert.FromBase64String(publicKeyCifrada), claveCifrado));
                                // Convierte la clave pública a formato RSA
                                var rsa = new RSACryptoServiceProvider();
                                rsa.FromXmlString(publicKey);

                                // Valida el token.
                                var principal = handler.ValidateToken(token, new TokenValidationParameters
                                {
                                    ValidateIssuerSigningKey = true,
                                    // Establece la clave que se debe usar para validar la firma del token.
                                    IssuerSigningKey = new RsaSecurityKey(rsa),
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

                // Pasa el control al siguiente middleware en la cadena.
                await next.Invoke();
            });
            return app;
        }
    }
}
