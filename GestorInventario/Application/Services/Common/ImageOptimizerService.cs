using GestorInventario.Interfaces.Application.Services;
using SkiaSharp;

namespace GestorInventario.Application.Services.Common
{
    public class ImageOptimizerService : IImageOptimizerService
    {
        private readonly ILogger<ImageOptimizerService> _logger;
        private readonly IWebHostEnvironment _environment;

        public ImageOptimizerService(ILogger<ImageOptimizerService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async Task<Stream> ProcessImageOnDemand(string imagePath, int? width = null, int? height = null, int quality = 50, bool isLcpCandidate = false)
        {
            try
            {
                var physicalPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));

                if (!File.Exists(physicalPath))
                {
                    _logger.LogWarning("Imagen no encontrada: {Path}", physicalPath);
                    return null;
                }

                // Cargar imagen con SkiaSharp
                using var inputStream = File.OpenRead(physicalPath);
                using var bitmap = SKBitmap.Decode(inputStream);

                if (bitmap == null)
                {
                    _logger.LogWarning("No se pudo decodificar la imagen: {Path}", physicalPath);
                    return null;
                }

                // Redimensionar manteniendo el aspect ratio
                SKBitmap resizedBitmap = bitmap;
                if (width.HasValue || height.HasValue)
                {
                    int targetWidth = width ?? bitmap.Width;
                    int targetHeight = height ?? bitmap.Height;

                    // Preservar aspect ratio
                    if (width.HasValue && !height.HasValue)
                    {
                        targetHeight = (int)(bitmap.Height * ((double)targetWidth / bitmap.Width));
                    }
                    else if (height.HasValue && !width.HasValue)
                    {
                        targetWidth = (int)(bitmap.Width * ((double)targetHeight / bitmap.Height));
                    }

                    // Usar Lanczos para LCP, Bicubic para el resto
                    var filterQuality = isLcpCandidate ? SKFilterQuality.High : SKFilterQuality.Medium;
                    resizedBitmap = bitmap.Resize(new SKImageInfo(targetWidth, targetHeight), filterQuality);

                    if (resizedBitmap != bitmap)
                    {
                        bitmap.Dispose(); // Liberar el original si se creó uno nuevo
                    }

                    _logger.LogDebug("Imagen redimensionada a {Width}x{Height}", resizedBitmap.Width, resizedBitmap.Height);
                }

                // Convertir a imagen y codificar
                using var image = SKImage.FromBitmap(resizedBitmap);
                var outputStream = new MemoryStream();

                var format = GetSkiaFormat(Path.GetExtension(imagePath).ToLowerInvariant());
                int effectiveQuality = isLcpCandidate ? Math.Max(quality, 60) : quality;

                var encodedData = EncodeImage(image, format, effectiveQuality);
                if (encodedData != null)
                {
                    encodedData.SaveTo(outputStream);
                    encodedData.Dispose();
                }

                outputStream.Position = 0;

                _logger.LogDebug("Imagen procesada: {Path} {Width}x{Height}, Tamaño: {Size} bytes",
                    imagePath, resizedBitmap.Width, resizedBitmap.Height, outputStream.Length);

                // Liberar recursos
                if (resizedBitmap != null && resizedBitmap != bitmap)
                {
                    resizedBitmap.Dispose();
                }

                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar imagen: {Path}", imagePath);
                throw;
            }
        }

        public async Task<string> OptimizeAndSaveImage(IFormFile imageFile, string folder = "imagenes", bool isLcpCandidate = false)
        {
            try
            {
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                var finalExtension = ShouldConvertToWebP(extension) ? ".webp" : extension;
                var fileName = $"{Guid.NewGuid()}{finalExtension}";
                var folderPath = Path.Combine(_environment.WebRootPath, folder);

                // Crear carpeta si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, fileName);

                // Cargar imagen desde el stream del archivo subido
                using var inputStream = imageFile.OpenReadStream();
                using var bitmap = SKBitmap.Decode(inputStream);

                if (bitmap == null)
                {
                    throw new InvalidOperationException($"No se pudo decodificar la imagen: {imageFile.FileName}");
                }

                // Redimensionar a un máximo de 388px para pantallas estándar, 776px para alta densidad
                int maxDimension = isLcpCandidate ? 776 : 388;
                SKBitmap finalBitmap = bitmap;

                if (bitmap.Width > maxDimension || bitmap.Height > maxDimension)
                {
                    var filterQuality = isLcpCandidate ? SKFilterQuality.High : SKFilterQuality.Medium;
                    finalBitmap = bitmap.Resize(new SKImageInfo(maxDimension, maxDimension), filterQuality);

                    if (finalBitmap != bitmap)
                    {
                        bitmap.Dispose();
                    }

                    _logger.LogDebug("Imagen redimensionada a {Width}x{Height} para optimización", finalBitmap.Width, finalBitmap.Height);
                }

                // Convertir a imagen y guardar
                using var image = SKImage.FromBitmap(finalBitmap);
                var format = GetSkiaFormat(finalExtension);
                int effectiveQuality = isLcpCandidate ? Math.Max(50, 60) : 50;

                var encodedData = EncodeImage(image, format, effectiveQuality);
                if (encodedData != null)
                {
                    using var fileStream = File.OpenWrite(filePath);
                    encodedData.SaveTo(fileStream);
                    encodedData.Dispose();
                }

                // Liberar recursos
                if (finalBitmap != null && finalBitmap != bitmap)
                {
                    finalBitmap.Dispose();
                }

                _logger.LogInformation("Imagen optimizada y guardada: {FilePath}, Tamaño: {Size} bytes",
                    filePath, new FileInfo(filePath).Length);

                return $"{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al optimizar imagen: {FileName}", imageFile.FileName);
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