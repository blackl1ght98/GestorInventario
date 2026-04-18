using AutoMapper;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Domain.Entities;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.user;
using Microsoft.EntityFrameworkCore;
namespace GestorInventario.Infraestructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IEmailService _emailService;
        private readonly HashService _hashService;      
        private readonly ILogger<AdminRepository> _logger;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;
        public AdminRepository(GestorInventarioContext context, IEmailService email, HashService hash,  ILogger<AdminRepository> logger, IMapper mapper,
            IUserRepository user)
        {
            _context = context;
            _emailService = email;
            _hashService = hash;         
            _logger = logger;
            _mapper = mapper;
            _userRepository = user;
        }


        public  OperationResult<IQueryable<Role>> ObtenerRoles()
        {
            var roles = _context.Roles.Include(x => x.Usuarios).AsQueryable();
            return OperationResult<IQueryable<Role>>.Ok("", roles);
        }
        public async Task<OperationResult<string>> CrearUsuario(UserViewModel model)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var existingUser = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (existingUser != null)
                    return OperationResult<string>.Fail("Ya existe un usuario registrado con este correo electrónico.");

                var resultadoHash = _hashService.Hash(model.Password);
                var usuarioDominio = _mapper.Map<EntityUser>(model);
                usuarioDominio.EstablecerPassword(resultadoHash.Hash, resultadoHash.Salt);

                var usuarioEf = _mapper.Map<Usuario>(usuarioDominio);
                await _context.AddEntityAsync(usuarioEf);

                var correo = await _emailService.SendEmailAsyncRegister(
                    new EmailDto { ToEmail = model.Email },
                    usuarioEf.Id);

                if (!correo.IsSuccess)
                {
                    return OperationResult<string>.Fail("Error al enviar el correo de confirmación.");
                }

                _logger.LogInformation("Correo de confirmación enviado a {Email}", usuarioDominio.Email);
                return OperationResult<string>.Ok("Correo de confirmacion enviado con exito");
            });
        }
        public async Task<OperationResult<string>> EditarUsuario(UsuarioEditViewModel userVM)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var resultado = await _userRepository.ObtenerUsuarioParaEdicionAsync(userVM.Id);
                if (resultado?.Data is null)
                    return OperationResult<string>.Fail(resultado?.Message ?? "Usuario no encontrado");

                if (!userVM.EsEdicionPropia && userVM.IdRol != resultado.Data.IdRol)
                {
                    if (userVM.IdRol == 0 || !await _context.Roles.AnyAsync(r => r.Id == userVM.IdRol))
                        return OperationResult<string>.Fail("El rol seleccionado no es válido");
                }

                await ActualizarUsuario(userVM, resultado.Data);
                return OperationResult<string>.Ok("Edicion realizada con exito");
            });
        }

        private async Task ActualizarUsuario(UsuarioEditViewModel userVM, EntityUser usuarioDominio)
        {
            string emailActual = usuarioDominio.Email;

            _mapper.Map(userVM, usuarioDominio);

            // Manejo especial del cambio de email
            if (emailActual != userVM.Email)
            {
                usuarioDominio.ActualizarEmail(userVM.Email);

                var correo = await _emailService.SendEmailAsyncRegister(
                    new EmailDto { ToEmail = userVM.Email },
                    usuarioDominio.Id);

                if (!correo.Success)
                    _logger.LogWarning("Error al enviar correo de confirmación: {Error}", correo.Message);
                else
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", userVM.Email);
            }

            // Cambio de rol (solo si es administrador editando a otro usuario)
            if (!userVM.EsEdicionPropia && usuarioDominio.IdRol != userVM.IdRol && userVM.IdRol != 0)
            {
                usuarioDominio.CambiarRol(userVM.IdRol);
            }

            var usuarioEf = await _context.Usuarios.FindAsync(usuarioDominio.Id);
            if (usuarioEf == null)
                throw new InvalidOperationException($"Usuario con ID {usuarioDominio.Id} no encontrado");

            _mapper.Map(usuarioDominio, usuarioEf);
            _context.EntityModified(usuarioEf);
            await _context.UpdateEntityAsync(usuarioEf);
        }
        public async Task<OperationResult<string>> EliminarUsuario(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var user = await _context.Usuarios.Include(x => x.Pedidos).FirstOrDefaultAsync(m => m.Id == id);
                if (user == null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                if (user.Pedidos.Any())
                {
                    return OperationResult<string>.Fail("El usuario no se puede eliminar porque tiene pedidos asociado");
                }

                await _context.DeleteEntityAsync(user);

                return OperationResult<string>.Ok("Usuario eliminado con exito");
            });
           
        } 
        public async Task<OperationResult<string>> BajaUsuario(int id)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuarioDB = await _userRepository.ObtenerUsuarioPorId(id);
                if (usuarioDB is null || usuarioDB.Data is null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.Data.BajaUsuario = true;

                await _context.UpdateEntityAsync(usuarioDB.Data);

                return OperationResult<string>.Ok("Usuario dado de baja con exito");

            });
            
        }
        public async Task<OperationResult<string>> AltaUsuario(int id)
        {

            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuarioDB = await _userRepository.ObtenerUsuarioPorId(id);
                if (usuarioDB is null || usuarioDB.Data is null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.Data.BajaUsuario = false;

                await _context.UpdateEntityAsync(usuarioDB.Data);

                return OperationResult<string>.Ok("Usuario dado de alta con exito");

            });
                
          }
          
        public async Task<OperationResult<Usuario>> ActualizarRolUsuario(int usuarioId, int rolId)
        {
            return await _context.ExecuteInTransactionAsync(async () =>
            {
                var usuario = await _userRepository.ObtenerUsuarioPorId(usuarioId);

                if (usuario is null || usuario.Data is null)
                {
                    return OperationResult<Usuario>.Fail("Usuario no encontrado");
                }
                var rol = await _context.Roles.FindAsync(rolId);
                if (rol == null)
                {
                    return OperationResult<Usuario>.Fail("Rol no encontrado");
                }

                usuario.Data.IdRol = rolId;
                await _context.UpdateEntityAsync(usuario.Data);              
                return OperationResult<Usuario>.Ok("", usuario.Data);
            });
           
        }
       

    }
       
}

