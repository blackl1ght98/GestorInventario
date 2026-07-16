namespace GestorInventario.Interfaces.Application.Common
{
    public interface IUrlService
    {
       
        string BuildUrl(string path);
        string GetPaypalReturnUrl();
        string GetPaypalCancelUrl();
    }
}
