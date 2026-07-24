using GestorInventario.Domain.enums.Email;

namespace GestorInventario.Interfaces.Application.Email
{
    public interface IBaseEmail
    {
        Task<bool> BuildEmail(string correo, string subject, EmailView view, object model);
    }
}
