namespace GestorInventario.Interfaces.Application
{
    public interface IImageOptimizerService
    {
        Task<Stream> ProcessImageOnDemand(string imagePath, int? width = null, int? height = null, int quality = 50, bool isLcpCandidate = false);
        Task<string> OptimizeAndSaveImage(IFormFile imageFile, string folder = "imagenes", bool isLcpCandidate = false);

    }
}
