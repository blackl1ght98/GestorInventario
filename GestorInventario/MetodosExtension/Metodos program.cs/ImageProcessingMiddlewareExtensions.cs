using GestorInventario.Application.Services;


namespace GestorInventario.Middlewares
{
    public static class ImageProcessingMiddlewareExtensions
    {
        public static IApplicationBuilder UseImageProcessing(
            this IApplicationBuilder app,
            string routePrefix = "/imagenes/")
        {
            return app.Use(async (context, next) =>
            {
                try
                {
                    await ProcessImageAsync(context, next, routePrefix);
                }
                catch (Exception ex)
                {
                    var logger = context.RequestServices.GetService<ILogger<ImageOptimizerService>>();
                    logger?.LogError(ex, "Error en el procesamiento de imagen para {Path}", context.Request.Path);

                    // Continuar con el siguiente middleware en caso de error
                    await next();
                }
            });
        }

        private static async Task ProcessImageAsync(HttpContext context, Func<Task> next, string routePrefix)
        {
            var path = context.Request.Path.Value;

            if (path.StartsWith(routePrefix) && context.Request.QueryString.HasValue)
            {
                var query = context.Request.Query;

                // Verificar si hay parámetros de procesamiento de imagen
                if (HasImageProcessingParameters(query))
                {
                    var imagePath = path.Split('?')[0];
                    var imageService = context.RequestServices.GetService<ImageOptimizerService>();

                    if (imageService != null)
                    {
                        var width = GetQueryIntValue(query, "width");
                        var height = GetQueryIntValue(query, "height");
                        var quality = GetQueryIntValue(query, "quality") ?? 80; // Valor por defecto 80%

                        var processedImage = await imageService.ProcessImageOnDemand(imagePath, width, height, quality);

                        if (processedImage != null)
                        {
                            context.Response.ContentType = GetContentType(imagePath);
                            context.Response.Headers["Cache-Control"] = "public, max-age=86400"; // Cache de 1 día
                            await processedImage.CopyToAsync(context.Response.Body);
                            return;
                        }
                    }
                }
            }

            await next();
        }

        private static bool HasImageProcessingParameters(IQueryCollection query)
        {
            return query.ContainsKey("width") ||
                   query.ContainsKey("height") ||
                   query.ContainsKey("quality");
        }

        private static int? GetQueryIntValue(IQueryCollection query, string key)
        {
            if (query.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            {
                return result;
            }
            return null;
        }

        private static string GetContentType(string path)
        {
            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".webp" => "image/webp",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}