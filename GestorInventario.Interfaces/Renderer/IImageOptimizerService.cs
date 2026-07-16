namespace GestorInventario.Interfaces.Renderer
{
    public interface IImageOptimizerService
    {
        Task<Stream> ProcessImageOnDemand(string imagePath, int? width = null, int? height = null, int quality = 50, bool isLcpCandidate = false);
        Task<string> OptimizeAndSaveImageAsync(
         byte[] imageBytes,
         string originalFileName,
         string folder = "imagenes",
         bool isLcpCandidate = false);

    }
}
