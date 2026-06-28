using GestorInventario.Domain.Models;
using GestorInventario.enums.Productos;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Barcode;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZXing;

namespace GestorInventario.Application.Services.Common
{
    public class BarCodeService : IBarCodeService
    {
        private readonly IProductoRepository _productoRepository; 
        private readonly ILogger<BarCodeService> _logger;
        private readonly IBarCodeImageRenderer _renderer;   
        private readonly IBarCodeImageStorage _storage;    
        public BarCodeService(IProductoRepository producto,  ILogger<BarCodeService> logger, IBarCodeImageRenderer renderer, IBarCodeImageStorage storage)
        {
            _productoRepository = producto;         
            _logger = logger;
            _renderer = renderer;
            _storage = storage;
        }

        public async Task<BarcodeResultDto> GenerateUniqueBarCodeAsync(BarcodeType type, string data, bool generateImage)
        {
            _logger.LogInformation("Generando código de barras. Tipo: {Type}, Generar imagen: {GenerateImage}", type, generateImage);

            string barcode = type switch
            {
                BarcodeType.UPC_A => await GenerateUniqueUPCACodeAsync(),
                BarcodeType.EAN_13 => GenerateEAN13Code(),
                BarcodeType.CODE_128 => GenerateCode128(data),
                _ => throw new ArgumentException("Tipo de código de barras no soportado.", nameof(type))
            };

            if (!generateImage)
                throw new Exception("Error al generar la imagen");

            var imageBytes = await _renderer.RenderAsync(barcode, type);
            var imagePath = await _storage.SaveAsync(imageBytes, ".png", "barcodes");
            return new BarcodeResultDto { Code = barcode, ImagePath = imagePath };
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



     

    }
}

