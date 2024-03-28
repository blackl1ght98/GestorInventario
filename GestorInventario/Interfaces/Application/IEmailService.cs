using GestorInventario.Application.DTOs;

namespace GestorInventario.Interfaces.Application
{
    public interface IEmailService
    {
        Task SendEmailAsyncRegister(DTOEmail userData);
        Task SendEmailAsyncResetPassword(DTOEmail userDataResetPassword);
    }
}
