using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using GestorInventario.Application.DTOS.Auth;
using GestorInventario.Application.DTOS.Email;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Application.Interfaces.Application.Services.Common;
using GestorInventario.Application.Interfaces.Application.Services.Email;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Web;

namespace GestorInventario.Application.Services.Authentication.Services
{
    public class AuthService : IAuthService
    {
       
        private readonly IHashService _hashService;
        private readonly IAdminRepository _adminRepository;
        private readonly ICurrentUserAccessor _currentUserAccesor;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        private readonly IHybridCacheService _cache;
        
        public AuthService( IHashService hashService, IAdminRepository adminRepository, ICurrentUserAccessor currentUser, 
            ILogger<AuthService> logger, IEmailService emailService, IHybridCacheService cache  )
        {
          
            _hashService = hashService;
            _adminRepository = adminRepository;
            _currentUserAccesor = currentUser;
            _logger = logger;
            _emailService = emailService;
            _cache = cache;
        }

        public async Task<OperationResult<Usuario>> Login(string email, LoginDto model)
        {
            var login = await _adminRepository.ObtenerEmail(email);
            if (login == null)
            {
                return OperationResult<Usuario>.Fail("El email y/o la contraseña son incorrectos. ");
            }
            if (!login.ConfirmacionEmail)
            {
                return OperationResult<Usuario>.Fail("Por favor, confirma tu correo electrónico antes de iniciar sesión.");


            }
            // Verificar si el usuario está dado de baja
            if (login.BajaUsuario)
            {
                return OperationResult<Usuario>.Fail("Su usuario ha sido dado de baja, contacte con el administrador.");


            }
            var resultadoHash = _hashService.Hash(model.Password, login.Salt);
            if (login.Password != resultadoHash.Hash)
            {
                return OperationResult<Usuario>.Fail("El email y/o la contraseña son incorrectos.");

            }
            return OperationResult<Usuario>.Ok( login);
        }
      
        public async Task<OperationResult<string>> SetNewPasswordAsync(SetNewPasswordDto cambio)
        {
           
            var resultadoValidacion = await ValidarTokenAsync(cambio.UserId, cambio.Token);
            if (!resultadoValidacion.Success)
                return OperationResult<string>.Fail(resultadoValidacion.Message);

            var usuarioDB = resultadoValidacion.Data;
            if (usuarioDB == null)
                return OperationResult<string>.Fail("Usuario no encontrado");

            if (string.IsNullOrEmpty(cambio.Password))
                return OperationResult<string>.Fail("La contraseña no puede estar vacía");

            var resultadoHashTemp = _hashService.Hash(cambio.TemporaryPassword, usuarioDB.ResetTokenSalt);
            if (usuarioDB.TemporaryPassword != resultadoHashTemp.Hash)
                return OperationResult<string>.Fail("La contraseña temporal no es válida");

            var resultadoHash = _hashService.Hash(cambio.Password);
            usuarioDB.Password = resultadoHash.Hash;
            usuarioDB.Salt = resultadoHash.Salt;

            //  Invalidar todo tras un cambio exitoso
            usuarioDB.EmailVerificationToken = null;
            usuarioDB.TemporaryPassword = null;
            usuarioDB.ResetTokenSalt = null;
            usuarioDB.FechaExpiracionContrasenaTemporal = null;
            usuarioDB.ResetTokenUsed = true;

            await _adminRepository.ActualizarUsuarioAsync(usuarioDB);
            return OperationResult<string>.Ok("Contraseña cambiada con exito");
        }
       
        private async Task<OperationResult<Usuario>> ValidarTokenAsync(int userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return OperationResult<Usuario>.Fail("El token no es valido");

            if (userId <= 0)
                return OperationResult<Usuario>.Fail("El usuario no es valido");

            var usuario = await _adminRepository.ObtenerUsuarioPorId(userId);
            if (usuario == null)
                return OperationResult<Usuario>.Fail("El usuario no existe");

            if (string.IsNullOrEmpty(usuario.EmailVerificationToken) ||
                !CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(usuario.EmailVerificationToken),
                    Encoding.UTF8.GetBytes(token)))
            {
                return OperationResult<Usuario>.Fail("El token no es valido");
            }

            if (usuario.FechaExpiracionContrasenaTemporal < DateTime.UtcNow)
            {
                usuario.FechaExpiracionContrasenaTemporal = null;
                usuario.TemporaryPassword = null;
                usuario.EmailVerificationToken = null;
                await _adminRepository.ActualizarUsuarioAsync(usuario);
                return OperationResult<Usuario>.Fail(
                    "El enlace y contraseña temporal han expirado. Solicite una nueva");
            }

            return OperationResult<Usuario>.Ok(usuario);
        }
        public async Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual)
        {
            
                var usuarioId = _currentUserAccesor.GetCurrentUserId();
                var usuarioDB = await _adminRepository.ObtenerUsuarioPorId(usuarioId);
                if (usuarioDB == null)
                {
                    return OperationResult<string>.Fail("Usuario no encontrado");
                }
                var anteriorpass = _hashService.Hash(passwordAnterior, usuarioDB.Salt);
                if (usuarioDB.Password != anteriorpass.Hash)
                {
                    return OperationResult<string>.Fail("Contraseña anterior incorrecta");
                }
                var actualPassword = _hashService.Hash(passwordActual);
                usuarioDB.Password = actualPassword.Hash;
                usuarioDB.Salt = actualPassword.Salt;
                await _adminRepository.ActualizarUsuarioAsync(usuarioDB);

                return OperationResult<string>.Ok("Contraseña cambiada con exito");
          

        }
        public async Task<OperationResult<RestoresPasswordDto>> PrepareRestorePassModel(int userId, string token)
        {
            try
            {
              
                var resultado = await ValidarTokenAsync(userId, token);
                if (!resultado.Success)
                    return OperationResult<RestoresPasswordDto>.Fail(resultado.Message);
                var res = new RestoresPasswordDto
                {
                    Token = token,
                    UserId = userId,
                };
                return OperationResult<RestoresPasswordDto>.Ok(res);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar el token de restauración de contraseña");
                return OperationResult<RestoresPasswordDto>.Fail("El servidor tardó mucho en responder, inténtelo de nuevo más tarde");
            }
        }

        public async Task<OperationResult<string>> EnviarCorreoResetAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                return OperationResult<string>.Fail("El correo proporcionado no es valido");

            var user = await _adminRepository.ObtenerEmail(email);

            if (user == null)
                return OperationResult<string>.Fail("No existe ningún usuario con ese correo");

            var userEmail = await _emailService.SendEmailAsyncResetPassword(new EmailDto
            {
                ToEmail = email
            }, user.Id);

            if (!userEmail.Success)
                _logger.LogError("Error al enviar el email: {error}", userEmail.Message);
            else
                _logger.LogInformation("Email de restablecimiento de contraseña enviado con éxito");
            
            return OperationResult<string>.Ok("Email enviado");
        }

        public async Task<OperationResult<Usuario>> CompletarMfaAsync(string userId, string codigo)
        {
            // Ajusta Usuario al tipo que use GenerarToken (entidad o DTO)

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(codigo))
                return OperationResult<Usuario>.Fail("Datos de verificación inválidos.");

            const int MaxAttempts = 3;

            string attemptsKey = $"MFA_Attempts_{userId}";
            string codeKey = $"MFA_{userId}";

            var attemptsStr = await _cache.GetStringAsync(attemptsKey);
            int attempts = string.IsNullOrEmpty(attemptsStr) ? 0 : int.Parse(attemptsStr);

             // Ya estaba bloqueado de antes
            if (attempts >= MaxAttempts)
            {
                await _cache.RemoveAsync(codeKey);
                await _cache.RemoveAsync(attemptsKey);
                return OperationResult<Usuario>.Fail(
                    "Número de intentos superado. Haga login de nuevo.");
            }

            var cachedCode = await _cache.GetStringAsync(codeKey);

            if (string.IsNullOrEmpty(codigo) || cachedCode != codigo)
            {
                attempts++;
                await _cache.SetStringAsync(attemptsKey, attempts.ToString(), TimeSpan.FromMinutes(5));

                // 3º fallo → bloqueo aquí, no en el siguiente request
                if (attempts >= MaxAttempts)
                {
                    await _cache.RemoveAsync(codeKey);
                    await _cache.RemoveAsync(attemptsKey);
                    return OperationResult<Usuario>.Fail(
                        "Número de intentos superado. Haga login de nuevo.");
                }

                int restantes = MaxAttempts - attempts;
                return OperationResult<Usuario>.Fail(
                    $"Código incorrecto. Le quedan {restantes} intento(s).");
            }

            // --- Éxito ---
            await _cache.RemoveAsync(attemptsKey);
            await _cache.RemoveAsync(codeKey);

            if (!int.TryParse(userId, out int id))
                return OperationResult<Usuario>.Fail("Usuario de verificación inválido.");

            var user = await _adminRepository.ObtenerUsuarioPorId(id); 
            if (user == null)
                return OperationResult<Usuario>.Fail("Usuario no encontrado.");

            return OperationResult<Usuario>.Ok(user, "MFA verificado correctamente.");
        }
    }
}
