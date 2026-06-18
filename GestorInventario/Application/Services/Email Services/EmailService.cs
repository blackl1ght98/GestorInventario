using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.enums.Email;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.ViewModels;
using GestorInventario.ViewModels.Email;
using GestorInventario.ViewModels.Paypal;
using GestorInventario.ViewModels.Pedidos;
using GestorInventario.ViewModels.Productos;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services
{
    public class EmailService:IEmailService
    {
        
          
       
        private readonly IUserRepository _userRepository;
        private readonly ILogger<EmailService> _logger;
        private readonly IPasswordResetService _password;
        private readonly IBaseEmail _baseemail;
        private readonly IUrlService _urlService;
        public EmailService(IUserRepository user,
            ILogger<EmailService> logger, IBaseEmail baseEmail,
           IPasswordResetService pass, IUrlService url)
        {
                 
            _logger = logger;
            _userRepository = user;
            _password = pass;   
            _baseemail = baseEmail;
            _urlService = url;
           
        }
        public async Task<OperationResult<string>> SendEmailAsyncRegister(EmailDto userDataRegister, int usuarioId)
        {
            try
            {   
                // Generar token
                string textoEnlace = GenerateSecureToken();    
                var resultadoToken = await _userRepository.ActualizarEmailVerificationTokenAsync(usuarioId, textoEnlace);
                if (!resultadoToken.Success)
                {
                    return OperationResult<string>.Fail("Error al actualizar el token de verificación");
                }
                // Construir el enlace de recuperación
                var model = new RegisterEmailViewmodel
                {
                    RecoveryLink = _urlService.BuildUrl(
                    $"/admin/confirm-registration/{usuarioId}/{textoEnlace}?redirect=true"),
                };
                var enviado=   await _baseemail.BuildEmail(userDataRegister.ToEmail, "Confirmar Email", EmailView.RegisterConfirmation, model);
                if (!enviado)
                {
                    return OperationResult<string>.Fail("Error al enviar el email");
                }
               return OperationResult<string>.Ok("Operacion realizada con exito");
            }
            catch (Exception ex)
            {
                return OperationResult<string>.Fail($"Ocurrio un error al enviar el email: {ex.Message}");
            }
        }
        private static string GenerateSecureToken(int byteLength = 32)
        {
            var bytes = new byte[byteLength];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
        public async Task<OperationResult<string>> SendEmailAsyncResetPassword(EmailDto userDataResetPassword, int usuarioId)
        {
            try
            {
                // 1. Llamada al repositorio para toda la lógica de BD
                var resultado = await _password.GenerarPasswordTemporalAsync(userDataResetPassword.ToEmail);

                if (!resultado.Success)
                {
                    return OperationResult<string>.Fail(resultado.Message);
                }
                string textoEnlace = GenerateSecureToken();
                var resultadoToken = await _userRepository.ActualizarEmailVerificationTokenAsync(usuarioId, textoEnlace);
         
                // 2. Solo preparar el modelo y enviar el correo
                var model = new ResetPasswordEmailViewmodel
                { 
                    RecoveryLink = _urlService.BuildUrl($"/auth/restore-password/{usuarioId}/{textoEnlace}?redirect=true"),
                    TemporaryPassword = resultado.Data
                };
                var enviado = await _baseemail.BuildEmail(userDataResetPassword.ToEmail, "Recuperar Contraseña", EmailView.PasswordReset, model);
                if (!enviado)
                {
                    return OperationResult<string>.Fail("Error al enviar el email");
                }

                return OperationResult<string>.Ok("Envio de correo exitoso", userDataResetPassword.ToEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de restablecimiento de contraseña");
                return OperationResult<string>.Fail($"Ocurrió un error al enviar el email: {ex.Message}");
            }
        }
      
        public async Task<OperationResult<string>> SendEmailAsyncLowStock(EmailDto correo, LowStockEmailData  producto)
        {

            // Crear el modelo para la vista del correo electrónico
            var model = new LowStockViewmodel
            {
                NombreProducto=producto.NombreProducto,
                Cantidad=producto.Cantidad,
                
            };
            var enviado = await _baseemail.BuildEmail(correo.ToEmail, "Alerta: Bajo stock de producto", EmailView.LowStock, model);
            if (!enviado)
            {
                return OperationResult<string>.Fail("Error al enviar el email");
            }
            return OperationResult<string>.Ok("Envio realizado con exito");
        }
        public async Task<OperationResult<string>> SendEmailAsyncFactura(EmailDto correo, string id)
        {
            // Crear el modelo para la vista del correo electrónico
            var model = new FacturaViewmodel
            {
                IdPago = id,
                EnlaceDescarga = _urlService.BuildUrl($"/Facturas/DownloadInvoice?id={id}"),

            };
            var enviado = await _baseemail.BuildEmail(correo.ToEmail, "Descargar factura", EmailView.Invoice, model);
            if (!enviado)
            {
                return OperationResult<string>.Fail("Error al enviar el correo");
            }
            return OperationResult<string>.Ok("Envio exitoso");
        }

        public async Task<OperationResult<string>> EnviarEmailSolicitudRembolso(EmailReembolsoAprobadoDto correo)
        {
            var administradores = await _userRepository.ObtenerEmailsAdministradoresAsync();

            // Validar contenido, no solo null
            if (administradores is null || administradores.Count == 0)
                return OperationResult<string>.Fail("No hay administradores para notificar.");

            // Limpiar: nulls, vacíos, duplicados
            var destinatarios = administradores
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (destinatarios.Count == 0)
                return OperationResult<string>.Fail("No hay emails válidos de administradores.");

            var model = new EmailRembolsoViewmodel
            {
                NumeroPedido = correo.NumeroPedido,
                NombreCliente = correo.NombreCliente,
                EmailCliente = correo.EmailCliente,
                FechaRembolso = correo.FechaRembolso,
                CantidadADevolver = correo.CantidadADevolver,
                MotivoRembolso = correo.MotivoRembolso,
                Productos = correo.Productos
            };

           
            var enviados = 0;
            var fallidos = new List<string>();

            foreach (var adminEmail in destinatarios)
            {
                try
                {
                    var ok = await _baseemail.BuildEmail(
                        adminEmail,
                        "Solicitud de rembolso",
                      EmailView.RefundRequest,
                        model);

                    if (ok) enviados++;
                    else fallidos.Add(adminEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando email de rembolso a {Email}", adminEmail);
                    fallidos.Add(adminEmail);
                   
                }
            }

            if (enviados == 0)
                return OperationResult<string>.Fail("No se pudo enviar el email a ningún administrador.");

            if (fallidos.Count > 0)
            {
                _logger.LogWarning("Email de rembolso enviado a {Enviados} de {Total}. Fallidos: {Fallidos}",
                    enviados, destinatarios.Count, string.Join(", ", fallidos));

                return OperationResult<string>.Ok(
                    $"Email enviado a {enviados} de {destinatarios.Count} administradores.");
            }

            return OperationResult<string>.Ok("Email enviado con éxito.");
        }
        public async Task<OperationResult<string>> EnviarNotificacionReembolsoAsync(EmailReembolsoAprobadoDto correo)
        {
            try
            {
               
                var viewmodel = new RembolsoAprobadoViewmodel
                {
                    NumeroPedido = correo.NumeroPedido,
                    NombreCliente = correo.NombreCliente,
                    EmailCliente = correo.EmailCliente,
                    FechaRembolso = correo.FechaRembolso,
                    CantidadADevolver = correo.CantidadADevolver,
                    MotivoRembolso = correo.MotivoRembolso,
                    Productos = correo.Productos,
                };
                var enviado = await _baseemail.BuildEmail(correo.EmailCliente, $"Tu reembolso para el pedido #{correo.NumeroPedido} ha sido aprobado", EmailView.RefundApproved, viewmodel);
                if (!enviado)
                {
                    return OperationResult<string>.Fail("Error al enviar el correo");
                }
                return OperationResult<string>.Ok("Envio exitoso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Se ha producido un error al enviar la notificacion");
                throw;
            }
        }
        public async Task<OperationResult<string>> SendMfaCodeEmail(string correo, string codigo)
        {
            try
            {
                
                var viewmodel = new OTPCode
                {
                    Email=correo,
                    OTP=codigo
                };
                var enviado = await _baseemail.BuildEmail(correo, "Codigo OTP", EmailView.OtpCode, viewmodel);
                if (!enviado)
                {
                    return OperationResult<string>.Fail("Error al enviar el correo");
                }
                return OperationResult<string>.Ok("Correo enviado con exito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Se ha producido un error al enviar la notificacion");
                throw;
            }
        }
       


    }
 }

