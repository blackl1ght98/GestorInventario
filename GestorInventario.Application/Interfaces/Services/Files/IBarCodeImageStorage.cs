namespace GestorInventario.Application.Interfaces.Services.Files
{
    public interface IBarCodeImageStorage
    {
        Task<string> SaveAsync(byte[] bytes, string extension, string folder);
    }
}
