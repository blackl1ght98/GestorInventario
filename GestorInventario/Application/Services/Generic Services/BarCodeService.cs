using GestorInventario.Application.DTOs.Barcode;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;

using ZXing;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class BarCodeService : IBarCodeService
    {
        private readonly GestorInventarioContext _context;
        private readonly IGestorArchivos _gestorArchivos;
        private readonly ILogger<BarCodeService> _logger;

        public BarCodeService(GestorInventarioContext context, IGestorArchivos gestorArchivos, ILogger<BarCodeService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _gestorArchivos = gestorArchivos ?? throw new ArgumentNullException(nameof(gestorArchivos));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BarcodeResult> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage)
        {
            _logger.LogInformation("Generando código de barras único. Tipo: {Type}, Generar imagen: {GenerateImage}", type, generateImage);

            string barcode = null;
            string imagePath = null;

            try
            {
                // Generar código según el tipo
                switch (type)
                {
                    case BarcodeType.UPC_A:
                        barcode = await GenerateUniqueUPCACodeAsync();
                        break;
                    case BarcodeType.EAN_13:
                        barcode = GenerateEAN13Code();
                        break;
                    case BarcodeType.CODE_128:
                        barcode = GenerateCode128(data);
                        break;
                    default:
                        _logger.LogError("Tipo de código de barras no soportado: {Type}", type);
                        throw new ArgumentException("Tipo de código de barras no soportado.", nameof(type));
                }

                // Generar imagen si se solicita
                if (generateImage)
                {
                    imagePath = await GenerateBarcodeImage(barcode, type);
                }

                return new BarcodeResult
                {
                    Code = barcode,
                    ImagePath = imagePath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el código de barras. Tipo: {Type}", type);
                throw;
            }
        }

        private async Task<string> GenerateUniqueUPCACodeAsync()
        {
            const int maxAttempts = 10;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                string code = GenerateUPCACode();
                _logger.LogDebug("Intento {Attempt} - Código generado: {Code}", attempts + 1, code);

                // Verificar unicidad
                var exists = await _context.Productos.AnyAsync(p => p.UpcCode == code);
                if (!exists)
                {
                    _logger.LogInformation("Código UPC-A único generado: {Code}", code);
                    return code;
                }

                _logger.LogWarning("Código UPC-A {Code} ya existe, generando uno nuevo.", code);
                attempts++;
            }

            _logger.LogError("No se pudo generar un código UPC-A único tras {MaxAttempts} intentos.", maxAttempts);
            throw new InvalidOperationException("No se pudo generar un código UPC-A único.");
        }

        private string GenerateUPCACode()
        {
            // Generar 11 dígitos aleatorios
            Random random = new Random();
            char[] digits = new char[11];
            for (int i = 0; i < 11; i++)
            {
                digits[i] = (char)(random.Next(0, 10) + '0');
            }

            // Calcular dígito de control
            int sum = 0;
            for (int i = 0; i < 11; i++)
            {
                int digit = digits[i] - '0';
                sum += (i % 2 == 0) ? digit * 3 : digit;
            }
            int checkDigit = (10 - (sum % 10)) % 10;

            return new string(digits) + checkDigit;
        }

        private string GenerateEAN13Code()
        {
            Random random = new Random();
            char[] digits = new char[12];
            for (int i = 0; i < 12; i++)
            {
                digits[i] = (char)(random.Next(0, 10) + '0');
            }

            int oddSum = 0;
            int evenSum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = digits[i] - '0';
                if (i % 2 == 0)
                    oddSum += digit;
                else
                    evenSum += digit;
            }
            int total = oddSum + evenSum * 3;
            int checkDigit = (10 - (total % 10)) % 10;

            return new string(digits) + checkDigit.ToString();
        }

        private string GenerateCode128(string data)
        {
           
            if (string.IsNullOrEmpty(data))
            {
                data = Guid.NewGuid().ToString();
                _logger.LogInformation("Generado GUID único para Code 128: {Data}", data);
            }
            return data;
        }

        private async Task<string> GenerateBarcodeImage(string barcode, BarcodeType type)
        {
            try
            {
                var barcodeWriter = new BarcodeWriterPixelData
                {
                    Format = type switch
                    {
                        BarcodeType.UPC_A => BarcodeFormat.UPC_A,
                        BarcodeType.EAN_13 => BarcodeFormat.EAN_13,
                        BarcodeType.CODE_128 => BarcodeFormat.CODE_128,
                        _ => throw new ArgumentException("Tipo de código de barras no soportado.", nameof(type))
                    },
                    Options = new ZXing.Common.EncodingOptions
                    {
                        Width = 300,
                        Height = 150,
                        Margin = 10
                    }
                };

                var pixelData = barcodeWriter.Write(barcode);
                using var ms = new MemoryStream();
                using var bitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                bitmap.UnlockBits(bitmapData);

                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var imageBytes = ms.ToArray();

                _logger.LogDebug("Guardando imagen del código de barras: {Barcode}", barcode);
                var imagePath = await _gestorArchivos.GuardarArchivo(imageBytes, ".png", "barcodes");
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar la imagen del código de barras: {Barcode}", barcode);
                throw;
            }
        }
    }
}
