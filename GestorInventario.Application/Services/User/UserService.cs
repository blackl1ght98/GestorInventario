using GestorInventario.Application.DTOS.Email;
using GestorInventario.Application.DTOS.User;
using GestorInventario.Application.Interfaces.Authentication;
using GestorInventario.Application.Interfaces.EmailServices;
using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Application.Interfaces.Services;
using GestorInventario.Application.Mappers;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;


namespace GestorInventario.Application.Services.User
{
    public class UserService : IUserService
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IHashService _hashService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;

        public UserService(
            IRoleRepository roleRepository,
            IHashService hashService,
            IEmailService emailService,
            IUserRepository user,
            ILogger<UserService> logger)
        {
            _roleRepository = roleRepository;
            _hashService = hashService;
            _emailService = emailService;
            _logger = logger;
            _userRepository = user;
        }

        public async Task<OperationResult<string>> CrearUsuarioAsync(RegisterUserDto model,
            string? rolSolicitado = null)
        {
            if (await _userRepository.ExisteEmailAsync(model.Email))
                return OperationResult<string>.Fail("Ya existe un usuario con este correo electrónico.");

            var esPrimerUsuario = !await _userRepository.AnyAsync();

            var nombreRol = esPrimerUsuario
                ? Roles.Administrador
                : (rolSolicitado ?? Roles.DefaultRegistro);

            var rol = await _roleRepository.GetRolByNameAsync(nombreRol);
            if (rol is null)
            {
                _logger.LogCritical("Rol crítico '{Rol}' no encontrado en base de datos. ¿Se ejecutó el seed?",
                    nombreRol);
                return OperationResult<string>.Fail(
                    "Error de configuración en el sistema. Contacte con el administrador.");
            }

            var hash = _hashService.Hash(model.Password);
            var usuarioEf = UserMapper.MapToDto(model);
            usuarioEf.Password = hash.Hash;
            usuarioEf.Salt = hash.Salt;
            usuarioEf.IdRol = rol.Id;
            usuarioEf.FechaRegistro = DateTime.UtcNow;
            usuarioEf.ConfirmacionEmail = esPrimerUsuario;

            var resultadoGuardado = await _userRepository.AgregarUsuarioAsync(usuarioEf);
            if (!resultadoGuardado.Success)
                return OperationResult<string>.Fail(resultadoGuardado.Message);

            if (!esPrimerUsuario)
            {
                var correo = await _emailService.SendEmailAsyncRegister(
                    new EmailDto { ToEmail = model.Email }, usuarioEf.Id);
                if (!correo.IsSuccess)
                    _logger.LogWarning("No se pudo enviar el email de confirmación");
            }

            _logger.LogInformation("Usuario {Email} creado con rol {Rol}", model.Email, nombreRol);
            return OperationResult<string>.Ok("Usuario creado correctamente");
        }

        public async Task<OperationResult<string>> EditarUsuarioAsync(EditUserDto userVm)
        {
            var resultado = await _userRepository.ObtenerUsuarioPorId(userVm.Id);
            if (resultado is null)
                return OperationResult<string>.Fail("Usuario no encontrado.");

            string emailActual = resultado.Email;
            bool cambioEmail = (emailActual != userVm.Email);

            // Actualizamos los datos generales del DTO (nombre, dirección, etc.)
            UpdateUsuarioFromDto(resultado, userVm);

            string mensajeRespuesta = "Edición realizada con éxito.";

            if (cambioEmail)
            {
                // 1. Guardamos el nuevo correo en la columna PENDIENTE, no en la oficial
                resultado.PendingEmail = userVm.Email; 
        
                // 2. REVERTIMOS temporalmente el Email oficial para que siga navegando con el viejo
                resultado.Email = emailActual; 

                resultado.ConfirmacionEmail = false;
                mensajeRespuesta = "Edición realizada con éxito. Como has cambiado tu correo electrónico, hemos enviado un enlace de confirmación a tu nueva dirección. Tu correo anterior seguirá activo hasta que verifiques el nuevo.";
            }

            var resultadoEdicion = await _userRepository.ActualizarUsuarioAsync(resultado);
            if (!resultadoEdicion.Success)
                return OperationResult<string>.Fail(resultadoEdicion.Message);

            if (cambioEmail)
            {
                // Pasamos el nuevo correo al servicio de envío de emails
                var correo = await _emailService.SendEmailAsyncConfirmEmail(
                    new EmailDto { ToEmail = userVm.Email }, resultado.Id);

                if (!correo.Success)
                    _logger.LogWarning("Error al enviar correo de confirmación: {Error}", correo.Message);
                else
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", userVm.Email);
            }

            return OperationResult<string>.Ok(mensajeRespuesta);
        }

        public async Task<OperationResult<Usuario>> EliminarUsuarioAsync(int id)
        {
            var usuario = await _userRepository.ObtenerUsuarioConProveedoresYPedidosAsync(id);

            if (usuario is null)
                return OperationResult<Usuario>.Fail();

            if (usuario.Pedidos.Any())
                return OperationResult<Usuario>.Fail("El usuario no se puede eliminar porque tiene pedidos asociados");

            if (usuario.Proveedores.Any())
                return OperationResult<Usuario>.Fail(
                    "El usuario no se puede eliminar porque tiene proveedores asociados");

            return await _userRepository.EliminarUsuarioAsync(usuario);
        }

        public async Task<OperationResult<string>> ValidarRegistro(ConfirmRegistrationDto confirmar)
        {
            var usuarioDb = await _userRepository.ObtenerUsuarioPorId(confirmar.UserId);

            if (usuarioDb is null)
            {
                _logger.LogWarning("Intento de confirmar un usuario inexistente con ID {UserId}", confirmar.UserId);
                return OperationResult<string>.Fail("Error al confirmar el usuario. Intentelo de nuevo mas tarde");
            }

            if (usuarioDb.ConfirmacionEmail)
            {
                _logger.LogInformation(
                    $"El usuario con email {usuarioDb.Email} ha intentado confirmar su correo estando confirmado");
                return OperationResult<string>.Fail("Usuario ya validado");
            }

            if (usuarioDb.EmailVerificationToken != confirmar.Token)
            {
                _logger.LogCritical("Intento de manipulacion del token por el usuario: " + usuarioDb.Id);
                return OperationResult<string>.Fail("Ocurrio un error al confirmar el usuario");
            }

            await ConfirmarEmail(confirmar.UserId);
            return OperationResult<string>.Ok("Validacion exitosa");
        }
        public async Task<OperationResult<string>> ValidarCorreo(ConfirmEmail confirmar)
        {
            var usuarioDb = await _userRepository.ObtenerUsuarioPorId(confirmar.UserId);

            if (usuarioDb is null)
            {
                _logger.LogWarning("Intento de confirmar un usuario inexistente con ID {UserId}", confirmar.UserId);
                return OperationResult<string>.Fail("Error al confirmar el usuario. Intentelo de nuevo mas tarde");
            }

            if (usuarioDb.ConfirmacionEmail && string.IsNullOrEmpty(usuarioDb.PendingEmail))
            {
                _logger.LogInformation("El usuario con email {Email} ha intentado confirmar su correo estando confirmado", usuarioDb.Email);
                return OperationResult<string>.Fail("Usuario ya validado");
            }

            if (usuarioDb.EmailVerificationToken != confirmar.Token)
            {
                _logger.LogCritical("Intento de manipulacion del token por el usuario: {UserId}", usuarioDb.Id);
                return OperationResult<string>.Fail("Ocurrio un error al confirmar el usuario");
            }

            // Si hay un correo pendiente de cambio, lo aplicamos oficialmente ahora
            if (!string.IsNullOrEmpty(usuarioDb.PendingEmail))
            {
                usuarioDb.Email = usuarioDb.PendingEmail; 
                usuarioDb.PendingEmail = null;           
            }

            // Marcamos como confirmado y limpiamos el token
            usuarioDb.ConfirmacionEmail = true;
            usuarioDb.EmailVerificationToken = null; 

            // Guardamos los cambios definitivos
            await _userRepository.ActualizarUsuarioAsync(usuarioDb); 

            _logger.LogInformation("Correo electrónico actualizado y confirmado con éxito para el usuario {UserId}", usuarioDb.Id);
            return OperationResult<string>.Ok("Validacion y cambio de correo exitosos");
        }
        private static void UpdateUsuarioFromDto(Usuario usuario, EditUserDto dto)
        {
            int.TryParse(dto.CodigoPostal, out int cp);
            usuario.Email = dto.Email;
            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.FechaNacimiento = dto.FechaNacimiento ?? default;
            usuario.Telefono = dto.Telefono ?? string.Empty;
            usuario.Direccion = dto.Direccion;
            usuario.Ciudad = dto.Ciudad;
            usuario.CodigoPostal = cp;
        }

        public async Task<OperationResult<string>> CambiarEstadoUsuarioAsync(int id, bool activar)
        {
            var usuario = await _userRepository.ObtenerUsuarioPorId(id);
            if (usuario == null) return OperationResult<string>.Fail();

            usuario.BajaUsuario = !activar;

            return await _userRepository.ActualizarUsuarioAsync(usuario);
        }

        private async Task ConfirmarEmail(int userId)
        {
            var usuarioUpdate = await _userRepository.ObtenerUsuarioPorId(userId);
            if (usuarioUpdate != null)
            {
                usuarioUpdate.ConfirmacionEmail = true;
                usuarioUpdate.EmailVerificationToken = null;
                await _userRepository.ActualizarUsuarioAsync(usuarioUpdate);
            }
        }
    }
}