using GestorInventario.Application.DTOs.Barcode;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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
            _context = context;
            _gestorArchivos = gestorArchivos;
            _logger = logger;
        }

        public async Task<BarcodeResult> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage)
        {
            _logger.LogInformation("Generando código de barras único. Tipo: {Type}, Generar imagen: {GenerateImage}", type, generateImage);
            string? imagePath=null;

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
                sumaPonderada += (i % 2 == 0) ? digito * 3 : digito; // Multiplicar por 3 en posiciones impares
            }

            // Calcular el dígito de control para que la suma total sea divisible por 10
            int digitoControl = (10 - (sumaPonderada % 10)) % 10;

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
            int sumaPosicionesImpares = 0; // Suma de dígitos en posiciones impares (1ª, 3ª, 5ª, ...)
            int sumaPosicionesPares = 0;   // Suma de dígitos en posiciones pares (2ª, 4ª, 6ª, ...)
            for (int i = 0; i < 12; i++)
            {
                int digito = digitos[i] - '0'; // Convertir carácter a número (ejemplo: '5' -> 5)
                if (i % 2 == 0)
                    sumaPosicionesImpares += digito; // Sumar dígitos en posiciones impares
                else
                    sumaPosicionesPares += digito;   // Sumar dígitos en posiciones pares
            }

            // Fórmula EAN-13: suma de impares + (suma de pares * 3)
            int sumaPonderada = sumaPosicionesImpares + (sumaPosicionesPares * 3);

            // Calcular el dígito de control para que la suma total sea divisible por 10
            int digitoControl = (10 - (sumaPonderada % 10)) % 10;

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
                // Validar el parámetro barcode
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

                // Generar los datos de píxeles del código de barras
                var pixelData = barcodeWriter.Write(barcode);
                if (pixelData == null || pixelData.Pixels == null || pixelData.Width <= 0 || pixelData.Height <= 0)
                {
                    _logger.LogError("Los datos del código de barras son inválidos (pixelData o Pixels nulo, o dimensiones inválidas) para: {Barcode}", barcode);
                    throw new InvalidOperationException("No se pudo generar la imagen del código de barras.");
                }

                // Crear una imagen Bitmap a partir de los datos de píxeles
                var pixels = pixelData.Pixels; 
                using var ms = new MemoryStream();
                using var bitmap = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, pixelData.Width, pixelData.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppRgb);

                // Copiar los píxeles del código de barras a la memoria del Bitmap
                // Marshal.Copy traslada los datos de píxeles (pixels, un byte[] en memoria administrada por .NET) 
                // a la memoria no administrada del Bitmap (bitmapData.Scan0, un IntPtr que apunta a la memoria nativa).
                // La memoria administrada es gestionada automáticamente por el recolector de basura de .NET, mientras 
                // que la memoria no administrada es gestionada por el sistema operativo (usada por System.Drawing.Bitmap 
                // para interactuar con APIs nativas de Windows como GDI+). Marshal.Copy actúa como un puente para copiar 
                // eficientemente los bytes entre estos dos entornos.
                Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
                bitmap.UnlockBits(bitmapData);

                // Guardar la imagen en formato PNG
                bitmap.Save(ms, ImageFormat.Png);
                var imageBytes = ms.ToArray();

                // Guardar la imagen en el sistema de archivos
                _logger.LogDebug("Guardando imagen del código de barras: {Barcode}", barcode);
                var imagePath = await _gestorArchivos.GuardarArchivo(imageBytes, ".png", "barcodes");
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
