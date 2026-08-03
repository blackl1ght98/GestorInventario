namespace GestorInventario.Interfaces.Application.Services.Common
{
    public interface IUrlService
    {
       
        string BuildUrl(string path);
        string GetPaypalReturnUrl();
        string GetPaypalCancelUrl();
    }
}
