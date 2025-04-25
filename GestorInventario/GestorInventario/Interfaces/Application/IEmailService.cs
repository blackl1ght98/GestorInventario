using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IEmailService
    {
        Task SendEmailAsyncRegister(DTOEmail userData);
        Task SendEmailAsyncResetPassword(DTOEmail userDataResetPassword);
        Task SendEmailAsyncLowStock(DTOEmail correo, Producto producto);
        Task SendEmailCreateProduct(DTOEmail correo, string productName);
        Task SendEmailAsyncResetPasswordOlvidada(DTOEmail userDataResetPassword);
    }
}
