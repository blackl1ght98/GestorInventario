namespace GestorInventario.Application.Interfaces.Services
{
    public interface IUrlService
    {
       
        string BuildUrl(string path);
        string GetPaypalReturnUrl();
        string GetPaypalCancelUrl();
    }
}
