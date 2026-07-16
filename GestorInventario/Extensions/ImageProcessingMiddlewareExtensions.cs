using GestorInventario.Interfaces.Renderer;
using Microsoft.AspNetCore.Http;

namespace GestorInventario.Extensions
{
    public static class ImageProcessingMiddlewareExtensions
    {
        private const int DefaultQuality = 80;
        private const int DefaultCacheSeconds = 300;
        private const string RoutePrefix = "/imagenes/";

        private static readonly Dictionary<string, string> AllowedContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".webp"] = "image/webp",
                [".png"] = "image/png",
                [".jpg"] = "image/jpeg",
                [".jpeg"] = "image/jpeg",
                [".gif"] = "image/gif",
            };

        public static IApplicationBuilder UseImageProcessing(
            this IApplicationBuilder app,
            string routePrefix = RoutePrefix)
        {
            return app.UseWhen(
                context => context.Request.Path.StartsWithSegments(routePrefix),
                appBuilder => appBuilder.Use(async (context, next) =>
                {
                    try
                    {
                        await ProcessImageAsync(context, next, routePrefix);
                    }
                    catch (Exception ex)
                    {
                        var logger = context.RequestServices
                            .GetRequiredService<ILogger<IImageOptimizerService>>();
                        logger.LogError(ex, "Error procesando imagen {Path}", context.Request.Path);

                        if (HasImageProcessingParameters(context.Request.Query))
                        {
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            return;
                        }

                        await next();
                    }
                }));
        }

        private static async Task ProcessImageAsync(
            HttpContext context,
            Func<Task> next,
            string routePrefix)
        {
            if (!HasImageProcessingParameters(context.Request.Query))
            {
                await next();
                return;
            }

            if (!TryGetContentType(context.Request.Path, out var contentType))
            {
                context.Response.StatusCode = StatusCodes.Status415UnsupportedMediaType;
                return;
            }

            var imageService = context.RequestServices
                .GetRequiredService<IImageOptimizerService>();

            var query = context.Request.Query;
            var width = GetQueryIntValue(query, "width");
            var height = GetQueryIntValue(query, "height");
            var quality = GetQueryIntValue(query, "quality") ?? DefaultQuality;

            var processedImage = await imageService.ProcessImageOnDemand(
                context.Request.Path, width, height, quality);

            if (processedImage is null)
            {
                await next();
                return;
            }

            context.Response.ContentType = contentType;
            context.Response.Headers["Cache-Control"] = $"public, max-age={DefaultCacheSeconds}";
            await processedImage.CopyToAsync(context.Response.Body);
        }

        private static bool HasImageProcessingParameters(IQueryCollection query)
        {
            return query.ContainsKey("width")
                || query.ContainsKey("height")
                || query.ContainsKey("quality");
        }

        private static int? GetQueryIntValue(IQueryCollection query, string key)
        {
            return query.TryGetValue(key, out var value) && int.TryParse(value, out var result)
                ? result
                : null;
        }

        private static bool TryGetContentType(PathString path, out string contentType)
        {
            var ext = Path.GetExtension(path.Value ?? string.Empty);
            return AllowedContentTypes.TryGetValue(ext, out contentType!);
        }
    }
}