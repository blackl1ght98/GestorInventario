using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.User;

using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.user;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GestorInventario.Infraestructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IHashService _hashService;        
        private readonly ILogger<AuthRepository> _logger;
        private readonly IUserRepository _userRepository;           
        private readonly ICurrentUserAccessor _currentUserAccessor;
        private readonly IEmailService _emailService;
        public AuthRepository(GestorInventarioContext context, IHashService hash, ICurrentUserAccessor current,
            ILogger<AuthRepository> logger, IUserRepository user, IEmailService email)
        {
            _context = context;
            _hashService = hash;
            _logger = logger;
            _userRepository = user;
                  
            _currentUserAccessor = current;
            _emailService = email;
        }

        public async Task<OperationResult<Usuario>> Login(string email, LoginViewModel model)
        {
            var login = await _context.Usuarios.Include(x => x.IdRolNavigation).FirstOrDefaultAsync(u => u.Email == email);
            if (login == null) {

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
            return OperationResult<Usuario>.Ok("",login);
        }
      
        private async Task<OperationResult<string>> ValidateResetTokenAsync(RestoresPasswordDto cambio)
        {
            try
            {
                var resultado = await ValidarTokenCambioPass(cambio);
                return OperationResult<string>.Ok("Validacion exitosa");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restaurar la contraseña");
                return OperationResult<string>.Fail("Ocurrió un error inesperado. Por favor, contacte al administrador o intente nuevamente.");
                
            }
        }
        public async Task<OperationResult<string>> SetNewPasswordAsync(RestoresPasswordDto cambio)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {

                var resultadoValidacion = await ValidarTokenCambioPass(cambio);

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

                await _context.UpdateEntityAsync(usuarioDB);               
               return OperationResult<string>.Ok("Contraseña cambiada con exito");
            });
           
        }


        public async Task<OperationResult<string>> ChangePassword(string passwordAnterior, string passwordActual)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuarioId = _currentUserAccessor.GetCurrentUserId();
                var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId);
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
                await _context.UpdateEntityAsync(usuarioDB);

                return OperationResult<string>.Ok("Contraseña cambiada con exito");
            });
           
        }
        private async Task<OperationResult<Usuario>> ValidarTokenCambioPass(RestoresPasswordDto cambio)
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
                    await _context.SaveChangesAsync();
                   
                    return OperationResult<Usuario>.Fail("El enlace y contraseña temporal han expirado. Solicite una nueva");
                };
                return OperationResult<Usuario>.Ok("Token valido", usuario.Data);        
           
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
                var resultado = await ValidateResetTokenAsync(cambio);
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
            var user= await _context.Usuarios.FirstOrDefaultAsync(x=>x.Email == email);

            var  userEmail = await _emailService.SendEmailAsyncResetPassword(new EmailDto
            {
                ToEmail = email
            },user.Id);

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
