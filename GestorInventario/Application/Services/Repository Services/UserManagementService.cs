using AutoMapper;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.DTOs.User;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.ViewModels.Usuarios;

namespace GestorInventario.Application.Services.User
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserRepository _usuarioRepository;
        private readonly IHashService _hashService;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<UserManagementService> _logger;
        private readonly IAdminRepository _adminRepository;
      
        public UserManagementService(
            IUserRepository usuarioRepository,
            IHashService hashService,
            IEmailService emailService,
            IMapper mapper,
            IAdminRepository admin,
            ILogger<UserManagementService> logger)
        {
            _usuarioRepository = usuarioRepository;
            _hashService = hashService;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
            _adminRepository = admin;
        }

        public async Task<OperationResult<string>> CrearUsuarioAsync(UserViewModel model)
        {
            var existing = await _usuarioRepository.ExisteEmailAsync(model.Email);
            if (existing)
                return OperationResult<string>.Fail("Ya existe un usuario con este correo electrónico.");

            var resultadoHash = _hashService.Hash(model.Password);
            var usuarioEf = _mapper.Map<Usuario>(model);
            usuarioEf.Password = resultadoHash.Hash;
            usuarioEf.Salt = resultadoHash.Salt;
            usuarioEf.FechaRegistro = DateTime.Now;
            var resultadoGuardado = await _usuarioRepository.AgregarUsuarioAsync(usuarioEf);
            if (!resultadoGuardado.Success)
                return OperationResult<string>.Fail(resultadoGuardado.Message);

            var correo = await _emailService.SendEmailAsyncRegister(
                new EmailDto { ToEmail = model.Email }, usuarioEf.Id);

            if (!correo.IsSuccess)
                _logger.LogWarning("No se pudo enviar el email de confirmación");

            _logger.LogInformation("Usuario {Email} creado correctamente", model.Email);
            return OperationResult<string>.Ok("Usuario creado y correo de confirmación enviado");
        }
        public async Task<OperationResult<string>> EditarUsuarioAsync(UsuarioEditViewModel userVM)
        {
            var resultado = await _usuarioRepository.ObtenerUsuarioPorId(userVM.Id);
            if (resultado is null)
                return OperationResult<string>.Fail("Usuario no encontrado");

           
            string emailActual = resultado.Email;

            _mapper.Map(userVM, resultado);

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
            return OperationResult<string>.Ok("Validacion exitosa");
        }
    }
}