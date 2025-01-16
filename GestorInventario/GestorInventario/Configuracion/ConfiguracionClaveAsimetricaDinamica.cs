using GestorInventario.Application.Services;
using GestorInventario.Interfaces.Application;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GestorInventario.Configuracion
{
    public static class ConfiguracionClaveAsimetricaDinamica
    {
        public static IServiceCollection ConfiguracionAsimetricaDinamica(this WebApplicationBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.SlidingExpiration = true;
            })
        .AddJwtBearer(options =>
        {
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    //Variable que almacena todas las cookies
                    var collectioncookies = context.Request.Cookies;
                    //Esta manera de poner las variables permite acceder a metodos creados por nosotros o metodos internos de .NET
                    //Variable que almacena la manera de acceder a IhttpcontectAccessor
                    var httpContextAccessor = context.HttpContext.RequestServices.GetRequiredService<IHttpContextAccessor>();
                    //Variable que almacena la manera de acceder a TokenService
                    var tokenservice = context.HttpContext.RequestServices.GetRequiredService<ITokenGenerator>();
                    //Variable que almacena la manera de acceder a IMemoryCache
                    var memoryCache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                    // Carga la clave pública cifrada desde las cookies
                    //var publicKeyCifrada = httpContextAccessor.HttpContext.Request.Cookies["PublicKey"];
                    //Obtiene el id del usuario de los claims del token
                    var userId = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    // Obtiene la clave de cifrado del usuario
                    if ( userId == null)
                    {
                        //Recorre la variable que almacena todas las cookies y....
                        foreach (var cookie in collectioncookies)
                        {
                            //elimina todas las cookies
                            context.Response.Cookies.Delete(cookie.Key);
                        }
                        //Si la ruta es distinta a "/Auth/Login"....
                        if (context.Request.Path != "/Auth/Login")
                        {
                            //redirige a "/Auth/Login"
                            context.Response.Redirect("/Auth/Login");
                        }
                    }
                    memoryCache.TryGetValue(userId.ToString() + "EncryptedAesKey", out byte[] claveCifrado);
                    if (claveCifrado == null  ||  userId==null)
                    {
                        //Recorre la variable que almacena todas las cookies y....
                        foreach (var cookie in collectioncookies)
                        {
                            //elimina todas las cookies
                            context.Response.Cookies.Delete(cookie.Key);
                        }
                        //Si la ruta es distinta a "/Auth/Login"....
                        if (context.Request.Path != "/Auth/Login")
                        {
                            //redirige a "/Auth/Login"
                            context.Response.Redirect("/Auth/Login");
                        }
                    }
                    // Carga la clave pública cifrada desde la memoria del servidor
                    memoryCache.TryGetValue(userId + "PublicKey", out byte[] publicKeyCifrada);

                  

                    //Si la claveCifrado es null....
                    if (claveCifrado == null || publicKeyCifrada == null)
                    {
                        //Recorre la variable que almacena todas las cookies y....
                        foreach (var cookie in collectioncookies)
                        {
                            //elimina todas las cookies
                            context.Response.Cookies.Delete(cookie.Key);
                        }
                        //Si la ruta es distinta a "/Auth/Login"....
                        if (context.Request.Path != "/Auth/Login")
                        {
                            //redirige a "/Auth/Login"
                            context.Response.Redirect("/Auth/Login");
                        }
                    }

                    // Descifra la clave pública
                    var publicKey = Encoding.UTF8.GetString(tokenservice.Descifrar(publicKeyCifrada, claveCifrado));

                    // Convierte la clave pública a formato RSA
                    var rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(publicKey);

                    // Configura las opciones para la validación del token JWT.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = builder.Configuration["JwtIssuer"]??Environment.GetEnvironmentVariable("JwtIssuer"),
                        ValidateAudience = true,
                        ValidAudience = builder.Configuration["JwtAudience"]??Environment.GetEnvironmentVariable("JwtAudience"),
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        // Establece la clave de firma del emisor
                        IssuerSigningKey = new RsaSecurityKey(rsa)
                    };

                    context.Token = context.Request.Cookies["auth"];
                    return Task.CompletedTask;
                },
            };
        });
            return builder.Services;
        }
    }
}
