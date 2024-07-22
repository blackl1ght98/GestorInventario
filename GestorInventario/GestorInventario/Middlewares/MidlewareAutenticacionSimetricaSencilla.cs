using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace GestorInventario.Middlewares
{
    public static class MidlewareAutenticacionSimetricaSencilla
    {
        public static IApplicationBuilder MiddlewareAutenticacionSimetricaSencilla(this IApplicationBuilder app, WebApplicationBuilder builder)
        {
            app.Use(async (context, next) =>
            {
                try
                {
                    var token = context.Request.Cookies["auth"];
                    var secret = builder.Configuration["ClaveJWT"];

                    // Si el token existe...
                    if (token != null)
                    {
                        // Crea un nuevo manejador de tokens JWT.
                        var handler = new JwtSecurityTokenHandler();
                        // Valida el token.
                        var principal = handler.ValidateToken(token, new TokenValidationParameters
                        {

                            ValidateIssuerSigningKey = true,
                            // Establece la clave que se debe usar para validar la firma del token.
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                            ValidateIssuer = false,
                            // ValidIssuer = builder.Configuration["JwtIssuer"],
                            ValidateAudience = false,
                            // ValidAudience = builder.Configuration["JwtAudience"],
                            //Toda esta configuracion se almacena en la variable validatedToken
                        }, out var validatedToken);


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
                // Obtiene el token de la cookie "auth".

            });
            return app;
           
        }
    }
}
