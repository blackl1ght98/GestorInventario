using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Application.Services.Authentication.Strategies.Configuration;

internal static class CookieAuthConfiguration
{
    public const string LoginPath = "/Auth/Login";
    public const string LogoutPath = "/Auth/Logout";
    public const string AccessDeniedPath = "/Auth/AccessDenied";

    public static AuthenticationBuilder AddBaseCookieAuth(
        this IServiceCollection services,
        CookieSecurePolicy securePolicy = CookieSecurePolicy.Always,
        bool includeAccessDeniedPath = true)
    {
        return services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = securePolicy;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
            options.SlidingExpiration = true;
            options.LoginPath = LoginPath;
            options.LogoutPath = LogoutPath;

            if (includeAccessDeniedPath)
            {
                options.AccessDeniedPath = AccessDeniedPath;
            }

            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                }
            };
        });
    }
}