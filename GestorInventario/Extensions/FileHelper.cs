namespace GestorInventario.Extensions;

 public static class FileHelper
    {
        public static async Task<byte[]?> ToBytesAsync(IFormFile? file)
        {
            if (file is null || file.Length == 0) return null;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }
    }