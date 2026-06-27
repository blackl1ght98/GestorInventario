using GestorInventario.Interfaces.Application.Services;
using SkiaSharp;

namespace GestorInventario.Application.Services.Common
{
    public class ImageOptimizerService : IImageOptimizerService
    {
        private readonly ILogger<ImageOptimizerService> _logger;
        private readonly IHostEnvironment _environment;

        public ImageOptimizerService(ILogger<ImageOptimizerService> logger, IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task<Stream> ProcessImageOnDemand(string imagePath, int? width = null, int? height = null, int quality = 50, bool isLcpCandidate = false)
        {
            try
            {
                // 1. RUTA ABSOLUTA: combinamos ContentRootPath + wwwroot + ruta relativa de la imagen.
                //    imagePath llega como "imagenes/xxx.webp" (sin slash inicial), TrimStart('/') lo protege
                //    por si llega con uno.
                var physicalPath = Path.Combine(
                    _environment.ContentRootPath, "wwwroot",
                    imagePath.TrimStart('/'));

                // 2. CASO BORDE: si el archivo no existe en disco, salimos con null y log de aviso.
                if (!File.Exists(physicalPath))
                {
                    _logger.LogWarning("Imagen no encontrada: {Path}", physicalPath);
                    return null;
                }

                // 3. DECODIFICAR: abrimos el archivo y SkiaSharp lo interpreta como bitmap.
                //    Soporta PNG, JPG, WebP, etc.
                using var inputStream = File.OpenRead(physicalPath);
                using var bitmap = SKBitmap.Decode(inputStream);

                // 4. CASO BORDE: si el formato no se reconoce, bitmap viene null y salimos.
                if (bitmap == null)
                {
                    _logger.LogWarning("No se pudo decodificar la imagen: {Path}", physicalPath);
                    return null;
                }

                // 5. BITMAP POR DEFECTO: empezamos apuntando al original.
                //    Solo creamos uno redimensionado si el caller pidió width o height.
                SKBitmap resizedBitmap = bitmap;
                if (width.HasValue || height.HasValue)
                {
                    // 6. DIMENSIONES OBJETIVO: si el caller solo pasó una, dejamos la otra como está.
                    int targetWidth = width ?? bitmap.Width;
                    int targetHeight = height ?? bitmap.Height;

                    // 7. ASPECT RATIO: si solo se pidió una dimensión, calculamos la otra
                    //    proporcionalmente para no deformar la imagen.
                    if (width.HasValue && !height.HasValue)
                    {
                        targetHeight = (int)(bitmap.Height * ((double)targetWidth / bitmap.Width));
                    }
                    else if (height.HasValue && !width.HasValue)
                    {
                        targetWidth = (int)(bitmap.Width * ((double)targetHeight / bitmap.Height));
                    }

                    // 8. CALIDAD DE MUESTREO: SKFilterQuality está deprecado en SkiaSharp 3.x.
                    //    MitchellNetravali ≈ Lanczos (alta calidad) para LCP,
                    //    Linear + MipmapMode.None ≈ Bicubic (rápido) para el resto.
                    var sampling = isLcpCandidate
                        ? new SKSamplingOptions(SKCubicResampler.Mitchell)
                        : new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

                    resizedBitmap = bitmap.Resize(new SKImageInfo(targetWidth, targetHeight), sampling);

                    // 9. LIBERAR ORIGINAL: si Resize creó uno nuevo, el original ya no se usa.
                    if (resizedBitmap != bitmap)
                    {
                        bitmap.Dispose();
                    }

                    _logger.LogDebug("Imagen redimensionada a {Width}x{Height}", resizedBitmap.Width, resizedBitmap.Height);
                }

                // 10. CONVERTIR A SKImage: el tipo que sabe codificar a PNG/JPG/WebP.
                using var image = SKImage.FromBitmap(resizedBitmap);

                // 11. STREAM DE SALIDA: aquí escribiremos los bytes codificados.
                var outputStream = new MemoryStream();

                // 12. FORMATO Y CALIDAD: respetamos el formato original del archivo
                //     (no convertimos a WebP aquí, solo redimensionamos y recomprimimos).
                var format = GetSkiaFormat(Path.GetExtension(imagePath).ToLowerInvariant());
                int effectiveQuality = isLcpCandidate ? Math.Max(quality, 60) : quality;

                // 13. CODIFICAR: comprimimos la imagen al formato elegido.
                var encodedData = EncodeImage(image, format, effectiveQuality);

                // 14. VOLCAR AL STREAM: escribimos los bytes comprimidos en outputStream.
                if (encodedData != null)
                {
                    encodedData.SaveTo(outputStream);
                    encodedData.Dispose();
                }

                // 15. REWIND: dejamos el cursor al principio para que el caller pueda leer desde el inicio.
                outputStream.Position = 0;

                _logger.LogDebug("Imagen procesada: {Path} {Width}x{Height}, Tamaño: {Size} bytes",
                    imagePath, resizedBitmap.Width, resizedBitmap.Height, outputStream.Length);

                // 16. LIBERAR BITMAP REDIMENSIONADO: si creamos uno nuevo en el paso 8.
                if (resizedBitmap != null && resizedBitmap != bitmap)
                {
                    resizedBitmap.Dispose();
                }

                // 17. DEVOLVER: el caller (controlador / middleware) lee el stream y lo envía al cliente.
                return outputStream;
            }
            catch (Exception ex)
            {
                // 18. LOG Y RE-LANZAR: igual que en OptimizeAndSaveImageAsync,
                //     no tragamos la excepción; el caller decide.
                _logger.LogError(ex, "Error al procesar imagen: {Path}", imagePath);
                throw;
            }
        }

        public async Task<string> OptimizeAndSaveImageAsync(
      byte[] imageBytes,
      string originalFileName,
      string folder = "imagenes",
      bool isLcpCandidate = false)
        {
            try
            {
                // 1. EXTENSIÓN: sacamos la extensión del nombre original y decidimos el formato final.
                //    Si no es ya WebP, la convertimos a WebP (más comprimido para fotos).
                var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
                var finalExtension = ShouldConvertToWebP(extension) ? ".webp" : extension;

                // 2. NOMBRE ÚNICO: generamos un GUID para evitar colisiones en disco.
                var fileName = $"{Guid.NewGuid()}{finalExtension}";

                // 3. RUTA DE DESTINO: combinamos ContentRootPath + wwwroot + carpeta.
                //    _environment.ContentRootPath apunta a la raíz de la aplicación.
                var folderPath = Path.Combine(_environment.ContentRootPath, "wwwroot", folder);

                // 4. CARPETA: si no existe, la creamos (idempotente, podemos llamarlo siempre).
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // 5. PATH FINAL: ruta absoluta donde escribiremos el archivo.
                var filePath = Path.Combine(folderPath, fileName);

                // 6. DECODIFICAR: envolvemos los bytes en un stream y SkiaSharp lo interpreta como imagen.
                //    Soporta PNG, JPG, WebP, etc. Devuelve null si el formato no se reconoce.
                using var inputStream = new MemoryStream(imageBytes);
                using var bitmap = SKBitmap.Decode(inputStream);

                // 7. CASO BORDE: si SkiaSharp no pudo decodificar la imagen, fallamos con mensaje claro.
                if (bitmap is null)
                {
                    throw new InvalidOperationException(
                        $"No se pudo decodificar la imagen: {originalFileName}");
                }

                // 8. DIMENSIÓN MÁXIMA: 776px para LCP (imagen destacada, alta densidad),
                //    388px para el resto (suficiente para pantalla normal).
                int maxDimension = isLcpCandidate ? 776 : 388;

                // 9. BITMAP POR DEFECTO: empezamos apuntando al original.
                //    Solo creamos uno nuevo si hace falta redimensionar.
                SKBitmap finalBitmap = bitmap;

                // 10. REDIMENSIONAR SOLO SI HACE FALTA: si ya es más pequeña, nos la saltamos
                //     para no perder calidad. SKFilterQuality está deprecado en SkiaSharp 3.x;
                //     usamos SKSamplingOptions con algoritmos explícitos.
                if (bitmap.Width > maxDimension || bitmap.Height > maxDimension)
                {
                    var sampling = isLcpCandidate
                        ? new SKSamplingOptions(SKCubicResampler.Mitchell)
                        : new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

                    finalBitmap = bitmap.Resize(
                        new SKImageInfo(maxDimension, maxDimension), sampling);

                    // 11. LIBERAR ORIGINAL: si Resize creó uno nuevo, el original ya no se usa.
                    if (finalBitmap != bitmap)
                    {
                        bitmap.Dispose();
                    }

                    _logger.LogDebug(
                        "Imagen redimensionada a {Width}x{Height}",
                        finalBitmap.Width, finalBitmap.Height);
                }

                // 12. CONVERTIR A SKImage: el tipo que sabe codificar a PNG/JPG/WebP.
                using var image = SKImage.FromBitmap(finalBitmap);

                // 13. FORMATO Y CALIDAD: mapeamos extensión → SKEncodedImageFormat y decidimos
                //     calidad de compresión (0-100). LCP exige mínimo 60, el resto vale con 50.
                var format = GetSkiaFormat(finalExtension);
                int effectiveQuality = isLcpCandidate ? Math.Max(50, 60) : 50;

                // 14. CODIFICAR: comprimimos la imagen al formato elegido. Devuelve los bytes (SKData).
                var encodedData = EncodeImage(image, format, effectiveQuality);

                // 15. ESCRIBIR A DISCO: abrimos el archivo en modo escritura y volcamos los bytes.
                if (encodedData is not null)
                {
                    using var fileStream = File.OpenWrite(filePath);
                    encodedData.SaveTo(fileStream);
                    encodedData.Dispose();
                }

                // 16. LIBERAR BITMAP REDIMENSIONADO: si creamos uno nuevo en el paso 10.
                if (finalBitmap != bitmap)
                {
                    finalBitmap.Dispose();
                }

                // 17. LOG DE AUDITORÍA: registramos ruta y tamaño final para debugging.
                _logger.LogInformation(
                    "Imagen optimizada y guardada: {FilePath}, Tamaño: {Size} bytes",
                    filePath, new FileInfo(filePath).Length);

                // 18. DEVOLVER RUTA RELATIVA: lo que se guarda en BD para mostrar después.
                //     Empieza con "imagenes/" porque es relativo a wwwroot.
                return $"{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                // 19. LOG Y RE-LANZAR: registramos el error pero NO lo tragamos.
                //     El controlador lo recibe y decide qué hacer (mostrar mensaje, redirigir, etc.).
                _logger.LogError(ex,
                    "Error al optimizar imagen: {FileName}", originalFileName);
                throw;
            }
        }



        private bool ShouldConvertToWebP(string extension)
        {
            return extension != ".webp";
        }

        private SKEncodedImageFormat GetSkiaFormat(string extension)
        {
            return extension switch
            {
                ".webp" => SKEncodedImageFormat.Webp,
                ".png" => SKEncodedImageFormat.Png,
                ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                _ => SKEncodedImageFormat.Webp
            };
        }

        private SKData EncodeImage(SKImage image, SKEncodedImageFormat format, int quality)
        {
            // Para WebP, SkiaSharp usa calidad 0-100 directamente
            // Para PNG, la calidad no aplica (siempre lossless)
            // Para JPEG, calidad 0-100

            return format switch
            {
                SKEncodedImageFormat.Webp => image.Encode(format, quality),
                SKEncodedImageFormat.Png => image.Encode(format, 100), // PNG es lossless, calidad no aplica
                SKEncodedImageFormat.Jpeg => image.Encode(format, quality),
                _ => image.Encode(format, quality)
            };
        }
    }
}