using GestorInventario.Domain.enums.Productos;
using GestorInventario.Interfaces.Renderer.Barcode;
using System.Runtime.InteropServices;
using SkiaSharp;
using ZXing;

namespace GestorInventario.Renderer.Barcode
{
    public class BarCodeImageRenderer : IBarCodeImageRenderer
    {

        public Task<byte[]> RenderAsync(string barcode, BarcodeType type)
        {
            if (string.IsNullOrEmpty(barcode))
                throw new ArgumentException("El código de barras no puede ser nulo ni estar vacío.", nameof(barcode));

            var format = type switch
            {
                BarcodeType.UPC_A => BarcodeFormat.UPC_A,
                BarcodeType.EAN_13 => BarcodeFormat.EAN_13,
                BarcodeType.CODE_128 => BarcodeFormat.CODE_128,
                _ => throw new ArgumentException("Tipo de código de barras no soportado.", nameof(type))
            };

            var writer = new BarcodeWriterPixelData
            {
                Format = format,
                Options = new ZXing.Common.EncodingOptions { Width = 300, Height = 150, Margin = 10 }
            };

            var pixelData = writer.Write(barcode);
            if (pixelData.Pixels == null || pixelData.Width <= 0 || pixelData.Height <= 0)
                throw new InvalidOperationException("No se pudo generar la imagen del código de barras.");

            int textAreaHeight = 25;
            int totalHeight = pixelData.Height + textAreaHeight;

            using var bitmapFinal = new SKBitmap(pixelData.Width, totalHeight, SKColorType.Rgba8888, SKAlphaType.Opaque);
            using var canvas = new SKCanvas(bitmapFinal);
            canvas.Clear(SKColors.White);

            // Copia directa de los pixeles generados por ZXing sobre la zona del código.
            var bitmapInfo = bitmapFinal.Info;
            int rowBytes = bitmapInfo.RowBytes;
            // Solo necesitamos copiar la región del barcode, sin la franja de texto.
            using var bitmapBarcode = new SKBitmap(pixelData.Width, pixelData.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
            var src = bitmapBarcode.GetPixels(out var srcLength);
            if (src == IntPtr.Zero || srcLength < pixelData.Pixels.Length)
                throw new InvalidOperationException("No se pudo reservar el buffer para el código de barras.");
            Marshal.Copy(pixelData.Pixels, 0, src, pixelData.Pixels.Length);

            canvas.DrawBitmap(bitmapBarcode, 0, 0);

            // Dibuja el texto bajo el código usando la tipografía por defecto del sistema (cross-platform).
            using var typeface = SKTypeface.Default;
            using var font = new SKFont(typeface, size: 14);
            using var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                
            };

            float textX = pixelData.Width / 2f;
            float textY = pixelData.Height + (textAreaHeight / 2f);

            canvas.DrawText(barcode, textX, textY, font, paint);

            using var image = SKImage.FromBitmap(bitmapFinal);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return Task.FromResult(data.ToArray());
        }
    }
}