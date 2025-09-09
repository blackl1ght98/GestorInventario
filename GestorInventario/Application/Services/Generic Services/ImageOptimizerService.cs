using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats;

namespace GestorInventario.Application.Services
{
    public class ImageOptimizerService
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

                using var image = await Image.LoadAsync(physicalPath);
                var outputStream = new MemoryStream();

                // Redimensionar manteniendo el aspect ratio
                if (width.HasValue || height.HasValue)
                {
                    var originalWidth = image.Width;
                    var originalHeight = image.Height;
                    int targetWidth = width ?? originalWidth;
                    int targetHeight = height ?? originalHeight;

                    // Preservar aspect ratio
                    if (width.HasValue && !height.HasValue)
                    {
                        targetHeight = (int)(originalHeight * ((double)targetWidth / originalWidth));
                    }
                    else if (height.HasValue && !width.HasValue)
                    {
                        targetWidth = (int)(originalWidth * ((double)targetHeight / originalHeight));
                    }

                    var resizeOptions = new ResizeOptions
                    {
                        Size = new Size(targetWidth, targetHeight),
                        Mode = ResizeMode.Max,
                        Sampler = isLcpCandidate ? KnownResamplers.Lanczos3 : KnownResamplers.Bicubic // Mejor calidad para LCP
                    };

                    image.Mutate(x => x.Resize(resizeOptions));
                    _logger.LogDebug("Imagen redimensionada a {Width}x{Height}", image.Width, image.Height);
                }

                // Strip metadata
                image.Metadata.ExifProfile = null;
                image.Metadata.IccProfile = null;
                image.Metadata.XmpProfile = null;

                // Guardar con compresión optimizada
                IImageEncoder encoder = GetEncoder(Path.GetExtension(imagePath).ToLowerInvariant(), quality, isLcpCandidate);
                await image.SaveAsync(outputStream, encoder);
                outputStream.Position = 0;

                _logger.LogDebug("Imagen procesada: {Path} {Width}x{Height}, Tamaño: {Size} bytes", imagePath, image.Width, image.Height, outputStream.Length);
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar imagen: {Path}", imagePath);
                throw;
            }
        }

        private IImageEncoder GetEncoder(string extension, int quality, bool isLcpCandidate)
        {
            // Usar calidad más alta para imágenes LCP
            int effectiveQuality = isLcpCandidate ? Math.Max(quality, 60) : quality;
            return extension switch
            {
                ".webp" => new WebpEncoder
                {
                    Quality = effectiveQuality,
                    Method = WebpEncodingMethod.Level6,
                    UseAlphaCompression = false, // Asegurar compresión con pérdida
                    FileFormat = WebpFileFormatType.Lossy,
                  
                },
                ".png" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
                ".jpg" or ".jpeg" => new JpegEncoder { Quality = effectiveQuality },
                _ => new WebpEncoder
                {
                    Quality = effectiveQuality,
                    Method = WebpEncodingMethod.Level6,
                    UseAlphaCompression = false,
                    FileFormat = WebpFileFormatType.Lossy
                }
            };
        }

        public async Task<string> OptimizeAndSaveImage(IFormFile imageFile, string folder = "imagenes", bool isLcpCandidate = false)
        {
            try
            {
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                var finalExtension = ShouldConvertToWebP(extension) ? ".webp" : extension;
                var fileName = $"{Guid.NewGuid()}{finalExtension}";
                var filePath = Path.Combine(_environment.WebRootPath, folder, fileName);

                using var image = await Image.LoadAsync(imageFile.OpenReadStream());

                // Redimensionar a un máximo de 388px para pantallas estándar, 776px para alta densidad
                int maxDimension = isLcpCandidate ? 776 : 388;
                if (image.Width > maxDimension || image.Height > maxDimension)
                {
                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(maxDimension, maxDimension),
                        Mode = ResizeMode.Max,
                        Sampler = isLcpCandidate ? KnownResamplers.Lanczos3 : KnownResamplers.Bicubic
                    }));
                }

                // Strip metadata
                image.Metadata.ExifProfile = null;
                image.Metadata.IccProfile = null;
                image.Metadata.XmpProfile = null;

                // Guardar optimizada
                IImageEncoder encoder = GetEncoder(finalExtension, 50, isLcpCandidate);
                await image.SaveAsync(filePath, encoder);

                _logger.LogInformation("Imagen optimizada y guardada: {FilePath}, Tamaño: {Size} bytes", filePath, new FileInfo(filePath).Length);
                return $"/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al optimizar imagen: {FileName}", imageFile.FileName);
                throw;
            }
        }

        public bool ShouldConvertToWebP(string extension)
        {
            return extension != ".webp";
        }
    }
}