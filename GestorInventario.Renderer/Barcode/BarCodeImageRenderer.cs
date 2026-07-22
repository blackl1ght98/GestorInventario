using GestorInventario.Domain.enums.Productos;
using GestorInventario.Interfaces.Renderer;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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

            using var bitmapFinal = new Bitmap(pixelData.Width, totalHeight, PixelFormat.Format32bppRgb);
            using var graphics = Graphics.FromImage(bitmapFinal);
            graphics.Clear(Color.White);

            using var bitmapBarcode = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppRgb);
            var bitmapData = bitmapBarcode.LockBits(
                new Rectangle(0, 0, pixelData.Width, pixelData.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppRgb);
            Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
            bitmapBarcode.UnlockBits(bitmapData);
            graphics.DrawImage(bitmapBarcode, 0, 0);

            using var font = new Font("Arial", 10, FontStyle.Regular);
            using var brush = new SolidBrush(Color.Black);
            var textSize = graphics.MeasureString(barcode, font);
            float textX = (pixelData.Width - textSize.Width) / 2;
            float textY = pixelData.Height + (textAreaHeight - textSize.Height) / 2;
            graphics.DrawString(barcode, font, brush, textX, textY);

            using var ms = new MemoryStream();
            bitmapFinal.Save(ms, ImageFormat.Png);
            return Task.FromResult(ms.ToArray());
        }
    }
}
