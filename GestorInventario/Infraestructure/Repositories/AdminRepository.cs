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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (existingUser != null)
                {
                    return OperationResult<string>.Fail("Ya existe un usuario registrado con este correo electrónico.");
                }

                var resultadoHash = _hashService.Hash(model.Password);

                // Mapeo a Entidad de Dominio
                var usuarioDominio = _mapper.Map<EntityUser>(model);

                // Asignamos contraseña y salt usando el método seguro
                usuarioDominio.EstablecerPassword(resultadoHash.Hash, resultadoHash.Salt);

                // Mapeo de Entidad de Dominio → Entidad EF para guardar
                var usuarioEf = _mapper.Map<Usuario>(usuarioDominio);

                await _context.AddEntityAsync(usuarioEf);
              

                var correo = await _emailService.SendEmailAsyncRegister(
                    new EmailDto { ToEmail = model.Email },
                    usuarioEf.Id);   

                if (correo.IsSuccess)
                {
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", usuarioDominio.Email);
                    await transaction.CommitAsync();
                    return OperationResult<string>.Ok("Correo de confirmacion enviado con exito");
                }
                else
                {
                    await transaction.RollbackAsync();
                    return OperationResult<string>.Fail("Error al enviar el correo de confirmación. Por favor, intenta de nuevo.");
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error crear el usuario");
                return OperationResult<string>.Fail("Hubo un error al crear el usuario, intentelo de nuevo mas tarde si el error persiste contacte con el equipo administrativo");
            }
        }
        public async Task<OperationResult<string>> EditarUsuario(UsuarioEditViewModel userVM)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Obtenemos la entidad de dominio
                var usuarioDominio = await _userRepository.ObtenerUsuarioParaEdicionAsync(userVM.Id);

                if (usuarioDominio is null || usuarioDominio.Data is null)
                {
                    return OperationResult<string>.Fail(usuarioDominio?.Message ?? "Usuario no encontrado");
                }           
                // Validar cambio de rol (solo si no es edición propia)
                if (!userVM.EsEdicionPropia && userVM.IdRol != usuarioDominio.Data.IdRol)
                {
                    if (userVM.IdRol == 0 || !await _context.Roles.AnyAsync(r => r.Id == userVM.IdRol))
                    {
                        return OperationResult<string>.Fail("El rol seleccionado no es válido");
                    }
                }

                await ActualizarUsuario(userVM, usuarioDominio.Data);

                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Edicion realizada con exito");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();

                // Reintento por concurrencia
                var resultadoUsuario = await _userRepository.ObtenerUsuarioParaEdicionAsync(userVM.Id);
                if (resultadoUsuario is null || resultadoUsuario.Data is null)
                {
                    return OperationResult<string>.Fail(resultadoUsuario?.Message ?? "Usuario no encontrado");
                }
                // Validar cambio de rol (solo si no es edición propia)
                if (!userVM.EsEdicionPropia && userVM.IdRol != resultadoUsuario.Data.IdRol)
                {
                    if (userVM.IdRol == 0 || !await _context.Roles.AnyAsync(r => r.Id == userVM.IdRol))
                    {
                        return OperationResult<string>.Fail("El rol seleccionado no es válido");
                    }
                }

                await ActualizarUsuario(userVM, resultadoUsuario.Data);
                await transaction.CommitAsync();

                return OperationResult<string>.Ok("Edicion realizada con exito");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al editar el usuario");
                return OperationResult<string>.Fail("Hubo un error al editar el usuario...");
            }
        }

        private async Task ActualizarUsuario(UsuarioEditViewModel userVM, EntityUser usuarioDominio)
        {
            try
            {
                // 1. Guardamos el email actual ANTES de mapear (esto es clave)
                string emailActual = usuarioDominio.Email;

                // 2. Mapeamos los campos básicos del ViewModel a la entidad de dominio
                _mapper.Map(userVM, usuarioDominio);

                // 3. Manejo especial del cambio de email
                if (emailActual != userVM.Email)
                {
                    usuarioDominio.ActualizarEmail(userVM.Email);

                    // Enviamos el correo de confirmación del nuevo email
                    var correo = await _emailService.SendEmailAsyncRegister(
                        new EmailDto { ToEmail = userVM.Email },
                        usuarioDominio.Id);     

                    if (!correo.Success)
                    {
                        _logger.LogWarning("Error al enviar correo de confirmación: {Error}", correo.Message);
                    }
                    else
                    {
                        _logger.LogInformation("Correo de confirmación enviado a {Email}", userVM.Email);
                    }
                }

                // 4. Actualizar rol solo si es un administrador y el rol cambió
                if (!userVM.EsEdicionPropia && usuarioDominio.IdRol != userVM.IdRol && userVM.IdRol != 0)
                {
                    usuarioDominio.CambiarRol(userVM.IdRol);
                }

                // 5. Convertimos la entidad de dominio a entidad EF y actualizamos
                var usuarioEf = await _context.Usuarios.FindAsync(usuarioDominio.Id);

                if (usuarioEf == null)
                {
                    throw new InvalidOperationException($"Usuario con ID {usuarioDominio.Id} no encontrado");
                }

                // Actualizamos la instancia existente de EF
                _mapper.Map(usuarioDominio, usuarioEf);

                _context.EntityModified(usuarioEf);
                await _context.UpdateEntityAsync(usuarioEf);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar usuario {UserId}", usuarioDominio.Id);
                throw;
            }
        }
        public async Task<OperationResult<string>> EliminarUsuario(int id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
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
                if (user.HistorialPedidos.Any())
                {
                    return OperationResult<string>.Fail("El usuario no se puede eliminar porque tiene historial de pedidos asociados.");
                   
                }
                await _context.DeleteEntityAsync(user);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Usuario eliminado con exito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar un usuario con {id}",id);
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Hubo un error al eliminar el usuario, intentelo de nuevo mas tarde si el error persiste contacte con el equipo administrativo");
              
               
            }
           
        } 
        public async Task<OperationResult<string>> BajaUsuario(int id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var usuarioDB = await _userRepository.ObtenerUsuarioPorId(id);
                if (usuarioDB is null || usuarioDB.Data is null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.Data.BajaUsuario = true;

                await   _context.UpdateEntityAsync(usuarioDB.Data);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Usuario dado de baja con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al dar de baja el usuario");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Hubo un error al dar de baja el usuario, intentelo de nuevo mas tarde si el error persiste contacte con el equipo administrativo");
               
            }
            
        }
        public async Task<OperationResult<string>> AltaUsuario(int id)
        {
            using var transaction=await _context.Database.BeginTransactionAsync();
            try
            {
                var usuarioDB = await _userRepository.ObtenerUsuarioPorId(id);
                if (usuarioDB is null || usuarioDB.Data is null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.Data.BajaUsuario = false;

                await  _context.UpdateEntityAsync(usuarioDB.Data);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Usuario dado de alta con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al dar de alta el usuario");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Hubo un error al dar de alta el usuario, intentelo de nuevo mas tarde si el error persiste contacte con el equipo administrativo");
               
            }
           
        }

        public async Task<OperationResult<Usuario>> ActualizarRolUsuario(int usuarioId, int rolId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var usuario = await _userRepository.ObtenerUsuarioPorId(usuarioId);
               // var usuario = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId);
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
                await transaction.CommitAsync();
                return OperationResult<Usuario>.Ok("",usuario.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrio un error inesperado al intentar actualizar el rol");

                await transaction.RollbackAsync();
                return OperationResult<Usuario>.Fail("Ocurrio un error al actualizar el usuario, intentelo de nuevo mas tarde si el error persiste contacte con el administrador");
            }
        }
       

    }
       
}

