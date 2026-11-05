using GestorInventario.Domain.enums.Email;

namespace GestorInventario.Application.Interfaces.EmailServices
{
    public interface IBaseEmail
    {
        Task<bool> BuildEmail(string correo, string subject, EmailView view, object model);
    }
}
