using GestorInventario.Application.DTOs.Email;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application
{
    public interface IEmailService
    {
        Task<OperationResult<string>> SendEmailAsyncRegister(EmailDto userDataRegister, Usuario usuarioDB);
        Task<OperationResult<string>> SendEmailAsyncResetPassword(EmailDto userDataResetPassword);
        Task SendEmailAsyncLowStock(EmailDto correo, Producto producto);
        Task SendEmailCreateProduct(EmailDto correo, string productName);     
        Task EnviarEmailSolicitudRembolso(EmailRembolsoDto correo);
        Task EnviarNotificacionReembolsoAsync(EmailReembolsoAprobadoDto correo);
        Task SendEmailAsyncFactura(EmailDto correo, string id);


    }
}
