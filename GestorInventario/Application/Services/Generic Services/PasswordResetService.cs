using GestorInventario.Application.DTOs.Email;
using GestorInventario.Interfaces.Application;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(IEmailService emailService, ILogger<PasswordResetService> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<(bool Success, string Error, string Email)> EnviarCorreoResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
            {
                return (false, "El correo electrónico no es válido.", null);
            }

            var (success, error, userEmail) = await _emailService.SendEmailAsyncResetPassword(new EmailDto
            {
                ToEmail = email
            });

            if (success)
            {
                _logger.LogInformation("Email de restablecimiento de contraseña enviado con éxito");
            }
            else
            {
                _logger.LogError("Error al enviar el email: {error}", error);
            }

            return (success, error, userEmail);
        }
    }

}
