using GestorInventario.Shared.DTOS.Email;
using GestorInventario.Shared.DTOS.Products;
using GestorInventario.Shared.Utilities;

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
