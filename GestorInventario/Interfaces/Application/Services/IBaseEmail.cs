using GestorInventario.enums.Email;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IBaseEmail
    {
        Task<bool> BuildEmail(string correo, string subject, EmailView view, object model);
    }
}
