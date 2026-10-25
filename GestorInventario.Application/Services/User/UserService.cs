using GestorInventario.Application.DTOS.Email;
using GestorInventario.Application.DTOS.User;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Application.Interfaces.Application.Services.Email;
using GestorInventario.Application.Interfaces.Application.Services.User;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
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
        private readonly IAdminRepository _adminRepository;

        public UserService(
            IRoleRepository roleRepository,
            IHashService hashService,
            IEmailService emailService,
            IAdminRepository admin,
            ILogger<UserService> logger)
        {
            _roleRepository = roleRepository;
            _hashService = hashService;
            _emailService = emailService;
            _logger = logger;
            _adminRepository = admin;
        }

        public async Task<OperationResult<string>> CrearUsuarioAsync(RegisterUserDto model,
            string? rolSolicitado = null)
        {
            if (await _adminRepository.ExisteEmailAsync(model.Email))
                return OperationResult<string>.Fail("Ya existe un usuario con este correo electrónico.");

            var esPrimerUsuario = !await _adminRepository.AnyAsync();

            // Bootstrap siempre gana: el primer usuario del sistema es admin, sin importar lo que manden
            var nombreRol = esPrimerUsuario
                ? Roles.Administrador
                : (rolSolicitado ?? Roles.DefaultRegistro);

            var rol = await _roleRepository.GetRolByNameAsync(nombreRol)
                      ?? throw new InvalidOperationException(
                          $"Rol '{nombreRol}' no existe. ¿Se ejecutó el seed?");

            var hash = _hashService.Hash(model.Password);
            var usuarioEf = CreateUsuarioFromDto(model);
            usuarioEf.Password = hash.Hash;
            usuarioEf.Salt = hash.Salt;
            usuarioEf.IdRol = rol.Id;
            usuarioEf.FechaRegistro = DateTime.UtcNow;
            usuarioEf.ConfirmacionEmail = esPrimerUsuario;

            var resultadoGuardado = await _adminRepository.AgregarUsuarioAsync(usuarioEf);
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
            var resultado = await _adminRepository.ObtenerUsuarioPorId(userVm.Id);
            if (resultado is null)
                return OperationResult<string>.Fail();


            string emailActual = resultado.Email;

            UpdateUsuarioFromDto(resultado, userVm);

            if (emailActual != userVm.Email)
            {
                resultado.Email = userVm.Email;
                resultado.ConfirmacionEmail = false;
            }

            var resultadoEdicion = await _adminRepository.ActualizarUsuarioAsync(resultado);
            if (!resultadoEdicion.Success)
                return OperationResult<string>.Fail(resultadoEdicion.Message);

            if (emailActual != userVm.Email)
            {
                var correo = await _emailService.SendEmailAsyncRegister(
                    new EmailDto { ToEmail = userVm.Email }, resultado.Id);

                if (!correo.Success)
                    _logger.LogWarning("Error al enviar correo de confirmación: {Error}", correo.Message);
                else
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", userVm.Email);
            }

            return OperationResult<string>.Ok("Edicion realizada con exito");
        }

        public async Task<OperationResult<Usuario>> EliminarUsuarioAsync(int id)
        {
            var usuario = await _adminRepository.ObtenerUsuarioConProveedoresYPedidosAsync(id);

            if (usuario is null)
                return OperationResult<Usuario>.Fail();

            if (usuario.Pedidos.Any())
                return OperationResult<Usuario>.Fail("El usuario no se puede eliminar porque tiene pedidos asociados");

            if (usuario.Proveedores.Any())
                return OperationResult<Usuario>.Fail(
                    "El usuario no se puede eliminar porque tiene proveedores asociados");

            return await _adminRepository.EliminarUsuarioAsync(usuario);
        }

        public async Task<OperationResult<string>> ValidarRegistro(ConfirmRegistrationDto confirmar)
        {
            var usuarioDb = await _adminRepository.ObtenerUsuarioPorId(confirmar.UserId);

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

            await ConfirmEmail(confirmar.UserId);
            usuarioDb.EmailVerificationToken = null;
            await _adminRepository.ActualizarUsuarioAsync(usuarioDb);
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
                Telefono = dto.Telefono,
                Direccion = dto.Direccion,
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
            var usuario = await _adminRepository.ObtenerUsuarioPorId(id);
            if (usuario == null) return OperationResult<string>.Fail();

            usuario.BajaUsuario = !activar; // Lógica de negocio

            return await _adminRepository.ActualizarUsuarioAsync(usuario);
        }
    
        private async Task ConfirmEmail(int userId)
        {
            var usuarioUpdate = await _adminRepository.ObtenerUsuarioPorId(userId);
            if (usuarioUpdate != null)
            {
                usuarioUpdate.ConfirmacionEmail = true;
                await _adminRepository.ActualizarUsuarioAsync(usuarioUpdate);
            }
        }
    }
}