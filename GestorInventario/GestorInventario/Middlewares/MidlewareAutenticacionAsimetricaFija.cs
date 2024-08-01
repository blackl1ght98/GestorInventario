using GestorInventario.Application.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Middlewares
{
    public static class MidlewareAutenticacionAsimetricaFija
    {
        public static IApplicationBuilder MiddlewareAutenticacionAsimetricaFija(this IApplicationBuilder app, WebApplicationBuilder builder)
        {

            //MIDDLEWARE PARA CLAVE ASIMETRICA FIJA MULTIUSUARIO
            app.Use(async (context, next) =>
            {

                try
                {
                    var token = context.Request.Cookies["auth"];

                    // Si el token existe...
                    if (token != null)
                    {
                        // Crea un nuevo manejador de tokens JWT.
                        var handler = new JwtSecurityTokenHandler();

                        // Carga la clave pública desde la configuración
                        var publicKey = Environment.GetEnvironmentVariable("PublicKey") ?? builder.Configuration["JWT:PublicKey"];

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
                            ValidIssuer = Environment.GetEnvironmentVariable("JwtIssuer") ?? builder.Configuration["JwtIssuer"],
                            ValidateAudience = true,
                            ValidAudience = Environment.GetEnvironmentVariable("JwtAudience") ?? builder.Configuration["JwtAudience"],
                        }, out var validatedToken);

                        // Establece el usuario del contexto actual a partir de la información del token.
                        context.User = principal;
                        token = context.Session.GetString("auth") ?? context.Request.Cookies["auth"];
                        context.Session.SetString("auth", token);
                    }

                    // Pasa el control al siguiente middleware en la cadena.
                    await next.Invoke();
                }
                catch (SecurityTokenException ex)
                {
                    var logger = log4net.LogManager.GetLogger(typeof(Program));
                    logger.Error("Error al validar el token", ex);

                }

            });

            return app;
        }
    }
}

