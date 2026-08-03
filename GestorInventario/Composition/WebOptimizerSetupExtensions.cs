using GestorInventario.Composition;

namespace GestorInventario.Composition
{
    public static class WebOptimizerSetupExtensions
    {
        public static IServiceCollection AddWebOptimizer(this IServiceCollection services)
        {
            services.AddWebOptimizer(pipeline =>
            {
                // Minificar CSS y JS
                pipeline.MinifyCssFiles("css/**/*.css");
                pipeline.MinifyJsFiles("js/**/*.js");

                // Agrupar archivos (bundle)
                pipeline.AddCssBundle("/css/bundle.css", "css/*.css");
                pipeline.AddJavaScriptBundle("/js/bundle.js", "js/*.js");
            });
            return services;
        }
    }
}
