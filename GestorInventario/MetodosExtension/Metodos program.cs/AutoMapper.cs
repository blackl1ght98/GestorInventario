using System.Reflection;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
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
