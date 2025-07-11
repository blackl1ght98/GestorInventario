using GestorInventario.Application.DTOs;

namespace GestorInventario.Interfaces.Application
{
    public interface IConfirmEmailService
    {
        Task ConfirmEmail(ConfirmRegistrationDto confirm);

    }
}
