using AutoMapper;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.ViewModels.user;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace GestorInventario.Infraestructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IEmailService _emailService;
        private readonly HashService _hashService;      
        private readonly ILogger<AdminRepository> _logger;
        private readonly IMapper _mapper;
        public AdminRepository(GestorInventarioContext context, IEmailService email, HashService hash,  ILogger<AdminRepository> logger, IMapper mapper )
        {
            _context = context;
            _emailService = email;
            _hashService = hash;         
            _logger = logger;
            _mapper = mapper;
        }

        public IQueryable<Usuario> ObtenerUsuarios()
        {
            return _context.Usuarios.Include(x => x.IdRolNavigation).AsQueryable();
        }
        public async Task<(Usuario?, string)> ObtenerPorId(int id)
        {
            var usuario = await _context.Usuarios
                .Include(x => x.IdRolNavigation)
                .FirstOrDefaultAsync(x => x.Id == id);

            return usuario is null
                ? (null, "Usuario no encontrado")
                : (usuario, "Usuario encontrado");
        }

        public async Task<List<Role>> ObtenerRoles() => await _context.Roles.ToListAsync();
        public async Task<(Usuario?,string)> ObtenerUsuarioConPedido(int id)
        {
            var usuario = await _context.Usuarios.Include(p => p.Pedidos).FirstOrDefaultAsync(m => m.Id == id);
            return usuario is null ? (null, "Este usuario no tiene pedidos") : (usuario, "Usuario con pedidos encontrado");
        }      
        public IQueryable<Role> ObtenerRolesConUsuarios()
        {
            return _context.Roles.Include(x => x.Usuarios).AsQueryable();
        }
        public IQueryable<Usuario> ObtenerUsuariosPorRol(int rolId)
        {
            return _context.Usuarios
                .Where(u => u.IdRol == rolId)
                .Include(u => u.IdRolNavigation)
                .AsQueryable();
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
                var user = _mapper.Map<Usuario>(model);
                user.Password = resultadoHash.Hash;
                user.Salt = resultadoHash.Salt;
                user.FechaRegistro = DateTime.Now;
                await _context.AddEntityAsync(user);
                var (success, error) = await _emailService.SendEmailAsyncRegister(new EmailDto { ToEmail = model.Email }, user);
                if (success)
                {
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", user.Email);
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

                _logger.LogError(ex, "Error crear el usuario");
                await transaction.RollbackAsync();
                return OperationResult<string>.Fail("Hubo un error al crear el usuario, intentelo de nuevo mas tarde si el error persiste contacte con el equipo administrativo");
               
            }
        }
        public async Task<OperationResult<string>> EditarUsuario(UsuarioEditViewModel userVM)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                

                var (user,mensaje) = await ObtenerPorId(userVM.Id);
                if (user == null)
                {
                    return OperationResult<string>.Fail(mensaje);
                }

                // Validar IdRol solo si es un administrador y se proporciona un nuevo rol
                if (!userVM.EsEdicionPropia && userVM.IdRol != user.IdRol)
                {
                    if (userVM.IdRol == 0 || !await _context.Roles.AnyAsync(r => r.Id == userVM.IdRol))
                    {
                        return OperationResult<string>.Fail("El rol seleccionado no es válido");
                    }
                }

                await ActualizarUsuario(userVM, user);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Edicion realizada con exito");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                var (user, mensaje) = await ObtenerPorId(userVM.Id);
                if (user == null)
                {
                    return OperationResult<string>.Fail(mensaje);
                }
                _context.ReloadEntity(user);
                await ActualizarUsuario(userVM, user);
                await transaction.CommitAsync();
                return OperationResult<string>.Ok("Edicion realizada con exito");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al editar el usuario: {Message}", ex.Message);
                return OperationResult<string>.Fail("Hubo un error al editar el usuario, intentelo de nuevo mas tarde si el error persiste contacte con el equipo administrativo");
               
            }
        }
        private async Task ActualizarUsuario(UsuarioEditViewModel userVM, Usuario user)
        {
            
            try
            {
                _mapper.Map(userVM, user);

                if (user.Email != userVM.Email)
                {
                    user.ConfirmacionEmail = false;
                    user.Email = userVM.Email;
                    var (success, error) = await _emailService.SendEmailAsyncRegister(new EmailDto { ToEmail = userVM.Email }, user);
                    if (!success)
                    {
                        _logger.LogWarning("Error al enviar correo de confirmación: {Error}", error);

                    }
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", user.Email);
                }

                // Actualizar IdRol solo si es un administrador, el rol cambió y es válido
                if (!userVM.EsEdicionPropia && user.IdRol != userVM.IdRol && userVM.IdRol != 0)
                {
                    user.IdRol = userVM.IdRol;
                }

                _context.EntityModified(user);
                await _context.UpdateEntityAsync(user);
               
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error al procesar usuario {UserId}", user.Id);

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
                var (usuarioDB, mensaje) = await ObtenerPorId(id);
                if (usuarioDB == null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.BajaUsuario = true;

                await   _context.UpdateEntityAsync(usuarioDB);
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
                var (usuarioDB, mensaje) = await ObtenerPorId(id);
                if (usuarioDB == null)
                {
                    return OperationResult<string>.Fail("El usuario no existe");
                }
                usuarioDB.BajaUsuario = false;

                await  _context.UpdateEntityAsync(usuarioDB);
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
              
        public async Task ActualizarRolUsuario(int usuarioId, int rolId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var (usuario, mensaje) = await ObtenerPorId(usuarioId); ;
                if (usuario == null)
                {
                    throw new Exception($"Usuario con Id {usuarioId} no encontrado.");
                }
                var rol = await _context.Roles.FindAsync(rolId);
                if (rol == null)
                {
                    throw new Exception($"Rol con Id {rolId} no encontrado.");
                }

                usuario.IdRol = rolId;
                await _context.UpdateEntityAsync(usuario);
                await transaction.CommitAsync();
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Ocurrio un error inesperado al intentar actualizar el rol");
                await transaction.RollbackAsync();
            }                       
        }
      

    }
       
}

