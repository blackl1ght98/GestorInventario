using GestorInventario.Application.DTOs.Barcode;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;

namespace GestorInventario.Application.Services.Common
{
    public class BarCodeService : IBarCodeService
    {
        private readonly IProductoRepository _productoRepository;
        private readonly IGestorArchivos _gestorArchivos;
        private readonly ILogger<BarCodeService> _logger;

        public BarCodeService(IProductoRepository producto, IGestorArchivos gestorArchivos, ILogger<BarCodeService> logger)
        {
            _productoRepository = producto;
            _gestorArchivos = gestorArchivos;
            _logger = logger;
        }

        public async Task<BarcodeResultDto> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage)
        {
            _logger.LogInformation("Generando código de barras único. Tipo: {Type}, Generar imagen: {GenerateImage}", type, generateImage);
            string? imagePath = null;

            try
            {

                string barcode;
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
                if (imagePath == null)
                {
                    throw new ArgumentNullException("La ruta de la imagen es nula");
                }
                return new BarcodeResultDto
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
                var exists = await _productoRepository.ObtenerCodigoUPC(code);
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
            Random generadorAleatorio = new Random();
            // Generar 11 dígitos aleatorios (0-9) como caracteres
            char[] digitos = new char[11];
            // Convierte número (0-9) a carácter ('0'-'9')
            for (int i = 0; i < 11; i++)
            {
                digitos[i] = (char)(generadorAleatorio.Next(0, 10) + '0');
            }

            // Calcular el dígito de control según el estándar UPC-A
            int sumaPonderada = 0; // Suma ponderada de dígitos
            for (int i = 0; i < 11; i++)
            {
                int digito = digitos[i] - '0'; // Convertir carácter a número (ejemplo: '5' -> 5)
                sumaPonderada += i % 2 == 0 ? digito * 3 : digito; // Multiplicar por 3 en posiciones impares
            }

            // Calcular el dígito de control para que la suma total sea divisible por 10
            int digitoControl = (10 - sumaPonderada % 10) % 10;

            // Combinar los 11 dígitos con el dígito de control
            return new string(digitos) + digitoControl;
        }

        private string GenerateEAN13Code()
        {
            Random generadorAleatorio = new Random();
            // Generar 12 dígitos aleatorios (0-9) como caracteres
            char[] digitos = new char[12];
            // Convierte número (0-9) a carácter ('0'-'9')
            for (int i = 0; i < 12; i++)
            {
                digitos[i] = (char)(generadorAleatorio.Next(0, 10) + '0');
            }

            // Calcular el dígito de control según el estándar EAN-13
            int sumaPosicionesImpares = 0; 
            int sumaPosicionesPares = 0;   
            for (int i = 0; i < 12; i++)
            {
                int digito = digitos[i] - '0'; 
                if (i % 2 == 0)
                    sumaPosicionesImpares += digito; 
                else
                    sumaPosicionesPares += digito;   
            }

            // Fórmula EAN-13: suma de impares + (suma de pares * 3)
            int sumaPonderada = sumaPosicionesImpares + sumaPosicionesPares * 3;

            // Calcular el dígito de control para que la suma total sea divisible por 10
            int digitoControl = (10 - sumaPonderada % 10) % 10;

            // Combinar los 12 dígitos con el dígito de control
            return new string(digitos) + digitoControl;
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
                if (string.IsNullOrEmpty(barcode))
                {
                    _logger.LogError("El código de barras proporcionado es nulo o está vacío.");
                    throw new ArgumentException("El código de barras no puede ser nulo ni estar vacío.", nameof(barcode));
                }

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
                if (pixelData == null || pixelData.Pixels == null || pixelData.Width <= 0 || pixelData.Height <= 0)
                {
                    _logger.LogError("Los datos del código de barras son inválidos para: {Barcode}", barcode);
                    throw new InvalidOperationException("No se pudo generar la imagen del código de barras.");
                }

                // Altura extra para el texto de los números
                int textAreaHeight = 25;
                int totalHeight = pixelData.Height + textAreaHeight;

                using var ms = new MemoryStream();

                // Bitmap final con espacio para el texto
                using var bitmapFinal = new Bitmap(pixelData.Width, totalHeight, PixelFormat.Format32bppRgb);
                using var graphics = Graphics.FromImage(bitmapFinal);

                // Fondo blanco
                graphics.Clear(Color.White);

                // Dibujar el código de barras en la parte superior
                using var bitmapBarcode = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
                var bitmapData = bitmapBarcode.LockBits(
                    new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppRgb);

                Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                bitmapBarcode.UnlockBits(bitmapData);

                graphics.DrawImage(bitmapBarcode, 0, 0);

                // Dibujar los números debajo del código de barras
                using var font = new Font("Arial", 10, FontStyle.Regular);
                using var brush = new SolidBrush(Color.Black);

                var textSize = graphics.MeasureString(barcode, font);
                float textX = (pixelData.Width - textSize.Width) / 2;
                float textY = pixelData.Height + (textAreaHeight - textSize.Height) / 2;

                graphics.DrawString(barcode, font, brush, textX, textY);

                // Guardar imagen final
                bitmapFinal.Save(ms, ImageFormat.Png);
                var imageBytes = ms.ToArray();
                var folder = "barcodes";
                _logger.LogDebug("Guardando imagen del código de barras: {Barcode}", barcode);
                var imagePath = await _gestorArchivos.GuardarArchivo(imageBytes, ".png",folder );

                if (string.IsNullOrEmpty(imagePath))
                {
                    _logger.LogError("No se pudo guardar la imagen del código de barras: {Barcode}", barcode);
                    throw new InvalidOperationException("No se pudo guardar la imagen del código de barras.");
                }
               
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

