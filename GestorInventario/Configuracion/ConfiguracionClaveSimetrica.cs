using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GestorInventario.Configuracion
{
    public static  class ConfiguracionClaveSimetrica
    {
        public static IServiceCollection ConfiguracionSimetrica(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            var secret = builder.Configuration["ClaveJWT"]??Environment.GetEnvironmentVariable("ClaveJWT");
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                // Configura las opciones para la validación del token JWT.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Activa la validación del emisor del token.
                    /*En un token JWT, el emisor (o “issuer”) es quien emite el token*/
                    /*ValidateIssuer = true: Esto verifica que el token fue creado por tu aplicación. Es como comprobar la 
                     * firma en una carta para asegurarte de que fue escrita por la persona correcta.*/
                    /*
                        El emisor (Issuer) es quien crea y firma el token, similar a la persona que escribe y envía una carta.
                        El público (Audience) es la entidad para la que se destina el token, similar a la persona que recibe la carta.

                    Al habilitar ValidateIssuer y ValidateAudience, estás asegurándote de que el token fue emitido por 
                    la entidad correcta (JwtIssuer) y está destinado a la aplicación correcta (JwtAudience). Esto es similar a verificar
                    el remitente y el destinatario de una carta para asegurarte de que fue enviada por la persona correcta 
                    y a la dirección correcta.
                     */
                    ValidateIssuer = true,
                    // Establece el emisor válido. El emisor es quien emite el token.
                    ValidIssuer = builder.Configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JwtIssuer"),
                    // Activa la validación del público del token.
                    /*Esto verifica que el token está destinado a ser usado con tu aplicación. Es como comprobar la dirección en un paquete para asegurarte de que fue enviado al lugar correcto.*/
                    ValidateAudience = true,
                    // Establece el público válido. El público es a quién está destinado el token.
                    ValidAudience = builder.Configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                    // Activa la validación de la vida útil del token.
                    ValidateLifetime = true,
                    // Activa la validación de la clave de firma del emisor.
                    ValidateIssuerSigningKey = true,
                    // Establece la clave de firma del emisor. La clave de firma se utiliza para verificar que el emisor
                    // del token es quien dice ser y para asegurar que el token no ha sido alterado en tránsito.
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
                // Configura los eventos que se pueden manejar durante el procesamiento del token JWT.
                options.Events = new JwtBearerEvents
                {
                    // Maneja el evento de recepción del mensaje(token).
                    OnMessageReceived = context =>
                    {
                        // Establece el token del contexto a partir de la cookie "auth".
                        context.Token = context.Request.Cookies["auth"];
                        // Completa la tarea.
                        return Task.CompletedTask;
                    },
                };
            });
            return builder.Services;
        }
    }
}

    

