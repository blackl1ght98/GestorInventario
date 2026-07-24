namespace GestorInventario.Interfaces.Application.Services.Files
{
    public interface IBarCodeImageStorage
    {
        Task<string> SaveAsync(byte[] bytes, string extension, string folder);
    }
}
