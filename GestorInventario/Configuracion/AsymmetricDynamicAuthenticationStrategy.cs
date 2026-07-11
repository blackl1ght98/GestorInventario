
using GestorInventario.Interfaces.Application.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace GestorInventario.Configuracion
{
    public class AsymmetricDynamicAuthenticationStrategy : IConfigurationAuthenticationStrategy
    {
        private readonly ILogger<AsymmetricDynamicAuthenticationStrategy> _logger;

        public AsymmetricDynamicAuthenticationStrategy(ILogger<AsymmetricDynamicAuthenticationStrategy> logger)
        {
            _logger = logger;
        }

        public void Configure(IServiceCollection services, IConfiguration configuration)
        {
            // Para claves dinámicas por usuario, no podemos configurar un IssuerSigningKey fijo
            // porque cada usuario tiene su propio par RSA. Por eso AddJwtBearer no se usa aquí:
            // la validación la hace el middleware DynamicAsymmetricAuthStrategy que consulta
            // la clave pública en caché por 'kid'.
            //
            // El esquema Cookie sigue siendo necesario para que [Authorize] sin política JWT
            // siga funcionando en endpoints estándar.

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.IsEssential = true;

                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";

                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;

                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        _logger.LogInformation("Redirigiendo a login desde cookie: {RedirectUri}",
                            context.RedirectUri);
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    }
                };
            });

            // NO registramos AddJwtBearer aquí. La autenticación JWT dinámica se hace
            // exclusivamente en DynamicAsymmetricAuthStrategy (middleware), que conoce el 'kid'
            // del header y busca la clave pública correspondiente en caché.
        }
    }
}