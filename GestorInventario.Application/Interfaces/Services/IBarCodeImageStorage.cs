namespace GestorInventario.Application.Interfaces.Services
{
    public interface IBarCodeImageStorage
    {
        Task<string> SaveAsync(byte[] bytes, string extension, string folder);
    }
}
