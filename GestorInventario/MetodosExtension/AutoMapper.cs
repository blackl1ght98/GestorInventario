using GestorInventario.MetodosExtension;
using System.Reflection;

namespace GestorInventario.MetodosExtension
{
    public static class AutoMapper
    {
        public static IServiceCollection AddAutoMapper(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.LicenseKey = Environment.GetEnvironmentVariable("LicenseKeyAutoMapper") ?? configuration["LicenseKeyAutoMapper"]; ;

                cfg.AddMaps(Assembly.GetExecutingAssembly());
            });
            return services;
        }
    }
}
