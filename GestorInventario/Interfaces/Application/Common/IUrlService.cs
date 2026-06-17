namespace GestorInventario.Interfaces.Application.Common
{
    public interface IUrlService
    {
        string GetBaseUrl();
        string BuildUrl(string path);
    }
}
