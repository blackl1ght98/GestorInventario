using GestorInventario.Application.DTOs.User;

namespace GestorInventario.Interfaces.Application
{
    public interface IConfirmEmailService
    {
        Task ConfirmEmail(ConfirmRegistrationDto confirm);

    }
}
