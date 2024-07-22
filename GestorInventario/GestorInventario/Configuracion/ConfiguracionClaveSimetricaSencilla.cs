using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GestorInventario.Configuracion
{
    public static class ConfiguracionClaveSimetricaSencilla
    {
        public static IServiceCollection ConfiguracionSimetricaSencilla(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            var secret = builder.Configuration["ClaveJWT"];
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddCookie(options =>
            {
                options.Cookie.HttpOnly = false;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                //options.LoginPath = "/Auth/Login";
                //options.LogoutPath = "/Auth/Logout";
                options.SlidingExpiration = true;
            })
          .AddJwtBearer(options =>
          {
              // Configura las opciones para la validación del token JWT.
              options.TokenValidationParameters = new TokenValidationParameters
              {

                  ValidateIssuer = false,
                  ValidateAudience = false,
                  ValidateLifetime = true,
                  ValidateIssuerSigningKey = true,

                  IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
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
