using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.ViewModels.user;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class AuthService : IAuthService
    {
       
        private readonly IHashService _hashService;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserAccessor _currentUserAccesor;
        private readonly ILogger<AuthService> _logger;
        private readonly IEmailService _emailService;
        public AuthService( IHashService hashService, IUserRepository userRepository, ICurrentUserAccessor currentUser, ILogger<AuthService> logger, IEmailService emailService  )
        {
          
            _hashService = hashService;
            _userRepository = userRepository;
            _currentUserAccesor = currentUser;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<OperationResult<Usuario>> Login(string email, LoginViewModel model)
        {
            var login = await _userRepository.ObtenerEmail(email);
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
            return OperationResult<Usuario>.Ok("", login);
        }
        public async Task<OperationResult<string>> SetNewPasswordAsync(RestoresPasswordDto cambio)
        {
            

                var resultadoValidacion = await ValidarTokenAsync(cambio);

                if (!resultadoValidacion.Success)
                    return OperationResult<string>.Fail(resultadoValidacion.Message);

                var usuarioDB = resultadoValidacion.Data;

                if (usuarioDB == null)
                    return OperationResult<string>.Fail("Usuario no encontrado");

                if (string.IsNullOrEmpty(cambio.Password))
                    return OperationResult<string>.Fail("La contraseña no puede estar vacía");

                var resultadoHashTemp = _hashService.Hash(cambio.TemporaryPassword, usuarioDB.Salt);
                if (usuarioDB.TemporaryPassword != resultadoHashTemp.Hash)
                    return OperationResult<string>.Fail("La contraseña temporal no es válida");

                var resultadoHash = _hashService.Hash(cambio.Password);
                usuarioDB.Password = resultadoHash.Hash;
                usuarioDB.Salt = resultadoHash.Salt;

                await _userRepository.ActualizarUsuarioAsync(usuarioDB);
                return OperationResult<string>.Ok("Contraseña cambiada con exito");
           

        }
        private async Task<OperationResult<Usuario>> ValidarTokenAsync(RestoresPasswordDto cambio)
        {
            var usuario = await _userRepository.ObtenerUsuarioPorId(cambio.UserId);
            if (usuario == null)
                return OperationResult<Usuario>.Fail("El usuario no existe");

            if (usuario.Data.EmailVerificationToken != cambio.Token)
                return OperationResult<Usuario>.Fail("El token no es valido");

            if (usuario.Data.FechaEnlaceCambioPass < DateTime.Now || usuario.Data.FechaExpiracionContrasenaTemporal < DateTime.Now)
            {
                usuario.Data.FechaEnlaceCambioPass = null;
                usuario.Data.FechaExpiracionContrasenaTemporal = null;
                usuario.Data.TemporaryPassword = null;
                await _userRepository.ActualizarUsuarioAsync(usuario.Data);

                return OperationResult<Usuario>.Fail("El enlace y contraseña temporal han expirado. Solicite una nueva");
            }    
            return OperationResult<Usuario>.Ok("Token valido", usuario.Data);

        }
        public async Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual)
        {
            
                var usuarioId = _currentUserAccesor.GetCurrentUserId();
                var usuarioDB = await _userRepository.ObtenerUsuarioPorId(usuarioId);
                if (usuarioDB == null)
                {
                    return OperationResult<string>.Fail("Usuario no encontrado");
                }
                var anteriorpass = _hashService.Hash(passwordAnterior, usuarioDB.Data.Salt);
                if (usuarioDB.Data.Password != anteriorpass.Hash)
                {
                    return OperationResult<string>.Fail("Contraseña anterior incorrecta");
                }
                var actualPassword = _hashService.Hash(passwordActual);
                usuarioDB.Data.Password = actualPassword.Hash;
                usuarioDB.Data.Salt = actualPassword.Salt;
                await _userRepository.ActualizarUsuarioAsync(usuarioDB.Data);

                return OperationResult<string>.Ok("Contraseña cambiada con exito");
          

        }
        public async Task<OperationResult<RestoresPasswordDto>> PrepareRestorePassModel(int userId, string token)
        {
            try
            {
                var cambio = new RestoresPasswordDto
                {
                    UserId = userId,
                    Token = token
                };
                var resultado = await ValidarTokenAsync(cambio);
                if (!resultado.Success)
                    return OperationResult<RestoresPasswordDto>.Fail(resultado.Message);

                return OperationResult<RestoresPasswordDto>.Ok("Modelo preparado con éxito", cambio);
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
            {
                return OperationResult<string>.Fail("El correo proporcionado no es valido");
            }
            var user = await _userRepository.ObtenerEmail(email);

            var userEmail = await _emailService.SendEmailAsyncResetPassword(new EmailDto
            {
                ToEmail = email
            }, user.Id);

            if (userEmail.Success)
            {
                _logger.LogInformation("Email de restablecimiento de contraseña enviado con éxito");
            }
            else
            {
                _logger.LogError("Error al enviar el email: {error}", userEmail);
            }
            return OperationResult<string>.Ok("", userEmail.Data);
        }
    }
}
