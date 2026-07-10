using GestorInventario.Application.AutoMapper;
using GestorInventario.AutoMapper;
using GestorInventario.Composition;


namespace GestorInventario.Composition
{
    public static class AutoMapperExtensions
    {
        public static IServiceCollection AddAutoMapper(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.LicenseKey = Environment.GetEnvironmentVariable("LicenseKeyAutoMapper") ?? configuration["LicenseKeyAutoMapper"]; ;

                //  cfg.AddMaps(Assembly.GetExecutingAssembly());
                cfg.AddMaps(
                    typeof(UserViewModelProfile).Assembly,
                    typeof(UserProfile).Assembly);
            });
      
            return services;
        }
    }
}
