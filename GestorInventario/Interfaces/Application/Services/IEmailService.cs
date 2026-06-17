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
        Task<OperationResult<string>> SendEmailAsyncLowStock(EmailDto correo, LowStockEmailData producto);

        Task<OperationResult<string>> EnviarEmailSolicitudRembolso(EmailReembolsoAprobadoDto correo);
        Task<OperationResult<string>> EnviarNotificacionReembolsoAsync(EmailReembolsoAprobadoDto correo);
        Task<OperationResult<string>> SendEmailAsyncFactura(EmailDto correo, string id);
        Task<OperationResult<string>> SendMfaCodeEmail(string correo, string codigo);


    }
}
