using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;

namespace GestorInventario.Interfaces.Application.Services
{
    public interface IEmailService
    {
        Task<OperationResult<string>> SendEmailAsyncRegister(EmailDto userDataRegister, int usuarioId);
        Task<OperationResult<string>> SendEmailAsyncResetPassword(EmailDto userDataResetPassword, int usuarioId);
        Task SendEmailAsyncLowStock(EmailDto correo, LowStockEmailData producto);
         
        Task EnviarEmailSolicitudRembolso(EmailRembolsoDto correo);
        Task EnviarNotificacionReembolsoAsync(EmailReembolsoAprobadoDto correo);
        Task SendEmailAsyncFactura(EmailDto correo, string id);


    }
}
