using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Middlewares
{
    public  static  class MidlewareAutenticacionSimetrica
    {
        public static IApplicationBuilder MiddlewareAutenticacionSimetrica(this IApplicationBuilder app, WebApplicationBuilder builder)
        {
            var secret = builder.Configuration["ClaveJWT"];
            //MIDDLEWARE CONFIGURADO PARA CLAVE SIMETRICA
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
                        // Valida el token.
                        var principal = handler.ValidateToken(token, new TokenValidationParameters
                        {
                            /*Esta parte se ejecuta cuando el usuario hace el login:
                            ValidateIssuerSigningKey = true: Valida que la firma del token es correcta.
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)): establece el como se tiene que validar el token
                            ValidateIssuer = true:  activa la validación del emisor del token para comprobar que el emisor es quien dice ser.
                            ValidIssuer = builder.Configuration["JwtIssuer"]: esto hace posible esa comprobacion de emisor
                            ValidateAudience = true:  Activa la validación del público del token, esto es para comprobar que el usuario tiene un token valido
                            ValidAudience = builder.Configuration["JwtAudience"]:  esto es el valor que se comprueba para ver si el token que tiene el
                            usuario es valido
                            */
                            // Valida la firma del token.
                            ValidateIssuerSigningKey = true,
                            // Establece la clave que se debe usar para validar la firma del token.
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                            ValidateIssuer = true,
                            ValidIssuer = builder.Configuration["JwtIssuer"],
                            ValidateAudience = true,
                            ValidAudience = builder.Configuration["JwtAudience"],
                            //Toda esta configuracion se almacena en la variable validatedToken
                        }, out var validatedToken);

                        // Establece el usuario del contexto actual a partir de la información del token.
                        //Detecta que usuario esta logueado, permitiendo hacer esa verificacion
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
