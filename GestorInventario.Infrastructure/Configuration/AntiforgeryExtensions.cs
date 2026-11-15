using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace GestorInventario.Infrastructure.Configuration
{
    public static class AntiforgeryExtensions
    {
        public static IServiceCollection ConfigureAntiforgery(this IServiceCollection services)
        {
           services.AddAntiforgery(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            return services;
        }
    }
}
