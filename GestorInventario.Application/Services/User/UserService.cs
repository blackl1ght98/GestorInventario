using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Interfaces.Application.Services.User;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Notifications.EmailServices;
using GestorInventario.Shared.DTOS.Email;
using GestorInventario.Shared.DTOS.User;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;


namespace GestorInventario.Application.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _usuarioRepository;
        private readonly IHashService _hashService;
        private readonly IEmailService _emailService;
        private readonly ILogger<UserService> _logger;
        private readonly IAdminRepository _adminRepository;

        public UserService(
            IUserRepository usuarioRepository,
            IHashService hashService,
            IEmailService emailService,
            IAdminRepository admin,

            ILogger<UserService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _hashService = hashService;
            _emailService = emailService;
            _logger = logger;
            _adminRepository = admin;

        }

        public async Task<OperationResult<string>> CrearUsuarioAsync(RegisterUserDto model, string? rolSolicitado = null)
        {
            if (await _usuarioRepository.ExisteEmailAsync(model.Email))
                return OperationResult<string>.Fail("Ya existe un usuario con este correo electrónico.");

            var esPrimerUsuario = !await _usuarioRepository.AnyAsync();

            // Bootstrap siempre gana: el primer usuario del sistema es admin, sin importar lo que manden
            var nombreRol = esPrimerUsuario
                ? Roles.Administrador
                : (rolSolicitado ?? Roles.DefaultRegistro);

            var rol = await _usuarioRepository.GetRolByNameAsync(nombreRol)
                ?? throw new InvalidOperationException(
                    $"Rol '{nombreRol}' no existe. ¿Se ejecutó el seed?");

            var hash = _hashService.Hash(model.Password);
            var usuarioEf = CreateUsuarioFromDto(model);
            usuarioEf.Password = hash.Hash;
            usuarioEf.Salt = hash.Salt;
            usuarioEf.IdRol = rol.Id;
            usuarioEf.FechaRegistro = DateTime.UtcNow;
            usuarioEf.ConfirmacionEmail = esPrimerUsuario;

            var resultadoGuardado = await _usuarioRepository.AgregarUsuarioAsync(usuarioEf);
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
        public async Task<OperationResult<string>> EditarUsuarioAsync(EditUserDto userVM)
        {
            var resultado = await _usuarioRepository.ObtenerUsuarioPorId(userVM.Id);
            if (resultado is null)
                return OperationResult<string>.Fail("Usuario no encontrado");

           
            string emailActual = resultado.Email;

            UpdateUsuarioFromDto(resultado, userVM);

            if (emailActual != userVM.Email)
            {
                resultado.Email = userVM.Email;
                resultado.ConfirmacionEmail = false;
            }

            var resultadoEdicion = await _usuarioRepository.ActualizarUsuarioAsync(resultado);
            if (!resultadoEdicion.Success)
                return OperationResult<string>.Fail(resultadoEdicion.Message);

            if (emailActual != userVM.Email)
            {
                var correo = await _emailService.SendEmailAsyncRegister(
                    new EmailDto { ToEmail = userVM.Email }, resultado.Id);

                if (!correo.Success)
                    _logger.LogWarning("Error al enviar correo de confirmación: {Error}", correo.Message);
                else
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", userVM.Email);
            }

            return OperationResult<string>.Ok("Edicion realizada con exito");
        }
        public async Task<OperationResult<string>> EliminarUsuarioAsync(int id)
        {
            var usuario = await _usuarioRepository.ObtenerUsuarioConProveedoresYPedidosAsync(id);

            if (usuario is null)
                return OperationResult<string>.Fail("El usuario no existe");

            if (usuario.Pedidos.Any())
                return OperationResult<string>.Fail("El usuario no se puede eliminar porque tiene pedidos asociados");

            if (usuario.Proveedores.Any())
                return OperationResult<string>.Fail("El usuario no se puede eliminar porque tiene proveedores asociados");

            return await _adminRepository.EliminarUsuario(id);
        }
        public async Task<OperationResult<string>> ValidarRegistro(ConfirmRegistrationDto confirmar)
        {
            var usuarioDB = await _usuarioRepository.ObtenerUsuarioPorId(confirmar.UserId);

            if (usuarioDB is null)
            {

                _logger.LogWarning("Intento de confirmar un usuario inexistente con ID {UserId}", confirmar.UserId);
                return OperationResult<string>.Fail("Error al confirmar el usuario. Intentelo de nuevo mas tarde"); 
            }

            if (usuarioDB.ConfirmacionEmail != false)
            {
             
                _logger.LogInformation($"El usuario con email {usuarioDB.Email} ha intentado confirmar su correo estando confirmado");
                return OperationResult<string>.Fail("Usuario ya validado");
            }
            if (usuarioDB.EmailVerificationToken != confirmar.Token)
            {
                _logger.LogCritical("Intento de manipulacion del token por el usuario: " + usuarioDB.Id);
                return OperationResult<string>.Fail("Ocurrio un error al confirmar el usuario");
            }
            await _usuarioRepository.ConfirmEmail(new ConfirmRegistrationDto
            {
                UserId = confirmar.UserId
            });
            usuarioDB.EmailVerificationToken = null;
            await _usuarioRepository.ActualizarUsuarioAsync(usuarioDB);
            return OperationResult<string>.Ok("Validacion exitosa");
        }
        // Crea un Usuario nuevo a partir de un RegisterUserDto.
        // Los campos sensibles (Password, Salt, IdRol, FechaRegistro, ConfirmacionEmail)
        // los asigna el caller porque dependen de hash/rol/estado de bootstrap.
        private static Usuario CreateUsuarioFromDto(RegisterUserDto dto)
        {
            return new Usuario
            {
                Email = dto.Email,
                NombreCompleto = dto.NombreCompleto,
                FechaNacimiento = dto.FechaNacimiento ?? default,
                Telefono = dto.Telefono ?? string.Empty,
                Direccion = dto.Direccion ?? "No especificada",
                Ciudad = dto.Ciudad,
                CodigoPostal = int.Parse(dto.CodigoPostal),
                BajaUsuario = false,
                ConfirmacionEmail = false,
                EmailVerificationToken = null,
                ResetTokenSalt = null,
                ResetTokenUsed = null,
                TemporaryPassword = null,
                FechaExpiracionContrasenaTemporal = null,
            };
        }

        // Copia los campos editables de EditUserDto sobre una entidad Usuario ya cargada.
        // NO toca Password, Salt, IdRol, ConfirmacionEmail, FechaRegistro ni Id.
        private static void UpdateUsuarioFromDto(Usuario usuario, EditUserDto dto)
        {
            usuario.Email = dto.Email;
            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.FechaNacimiento = dto.FechaNacimiento ?? default;
            usuario.Telefono = dto.Telefono ?? string.Empty;
            usuario.Direccion = dto.Direccion;
            usuario.Ciudad = dto.Ciudad;
            usuario.CodigoPostal = int.Parse(dto.CodigoPostal);
        }
    }

}