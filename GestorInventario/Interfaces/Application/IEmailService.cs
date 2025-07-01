using GestorInventario.Application.DTOs;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IEmailService
    {
        Task<(bool, string)> SendEmailAsyncRegister(DTOEmail userDataRegister);
        Task<(bool, string, string)> SendEmailAsyncResetPassword(DTOEmail userDataResetPassword);
        Task SendEmailAsyncLowStock(DTOEmail correo, Producto producto);
        Task SendEmailCreateProduct(DTOEmail correo, string productName);
     
        Task SendEmailAsyncRembolso(DTOEmailRembolso correo);
    }
}
