namespace GestorInventario.Application.Interfaces.Services.Common
{
    public interface IUrlService
    {
       
        string BuildUrl(string path);
        string GetPaypalReturnUrl();
        string GetPaypalCancelUrl();
    }
}
