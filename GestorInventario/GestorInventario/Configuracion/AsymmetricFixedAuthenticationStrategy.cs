using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace GestorInventario.Configuracion.Strategies
{
    public class AsymmetricFixedAuthenticationStrategy : IAuthenticationStrategy
    {
        public IServiceCollection ConfigureAuthentication(WebApplicationBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.SlidingExpiration = true;
            })
            .AddJwtBearer(options =>
            {
                var publicKey = configuration["Jwt:PublicKey"] ?? Environment.GetEnvironmentVariable("PublicKey");
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = configuration["JwtIssuer"] ?? Environment.GetEnvironmentVariable("JwtIssuer"),
                    ValidateAudience = true,
                    ValidAudience = configuration["JwtAudience"] ?? Environment.GetEnvironmentVariable("JwtAudience"),
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa)
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        context.Token = context.Request.Cookies["auth"];
                        return Task.CompletedTask;
                    }
                };
            });

            return builder.Services;
        }
    }
}