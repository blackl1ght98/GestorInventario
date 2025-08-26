using GestorInventario.Application.DTOs.Email;
using GestorInventario.Domain.Models;

namespace GestorInventario.Interfaces.Application
{
    public interface IEmailService
    {
        Task<(bool, string)> SendEmailAsyncRegister(EmailDto userDataRegister, Usuario usuarioDB);
        Task<(bool, string, string)> SendEmailAsyncResetPassword(EmailDto userDataResetPassword);
        Task SendEmailAsyncLowStock(EmailDto correo, Producto producto);
        Task SendEmailCreateProduct(EmailDto correo, string productName);     
        Task EnviarEmailSolicitudRembolso(EmailRembolsoDto correo);
        Task EnviarNotificacionReembolsoAsync(EmailReembolsoAprobadoDto correo);
    }
}
