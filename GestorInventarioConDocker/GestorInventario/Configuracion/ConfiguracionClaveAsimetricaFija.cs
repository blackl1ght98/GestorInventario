using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace GestorInventario.Configuracion
{
    public static class ConfiguracionClaveAsimetricaFija
    {
        public static IServiceCollection ConfiguracionAsimetricaFija(this WebApplicationBuilder builder, IConfiguration configuration)
        {
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
                // Carga la clave pública desde la configuración
                var publicKey = builder.Configuration["Jwt:PublicKey"];

                // Convierte la clave pública a formato RSA
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);

                // Configura las opciones para la validación del token JWT.
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["JwtIssuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["JwtAudience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    // Establece la clave de firma del emisor
                    IssuerSigningKey = new RsaSecurityKey(rsa)
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["auth"];
                        return Task.CompletedTask;
                    },
                };
            });
            return builder.Services;
        }
    }
}

    

