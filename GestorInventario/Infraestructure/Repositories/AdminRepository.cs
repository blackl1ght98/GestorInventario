using AutoMapper;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.DTOs.Email;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
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
        public async Task<List<Permiso>> ObtenerPermisos() => await _context.Permisos.ToListAsync();
        public async Task<(bool, string)> CrearUsuario(UserViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (existingUser != null)
                {
                    return (false, "El Email ya existe en base de datos");
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
                    return (true, "Correo de confirmacion enviado con exito");
                }
                else
                {
                    await transaction.RollbackAsync();
                    return (false, "Error al enviar el correo de confirmación. Por favor, intenta de nuevo.");
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error crear el usuario");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }
        }
        public async Task<(bool, string?)> EditarUsuario(UsuarioEditViewModel userVM)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                

                var (user,mensaje) = await ObtenerPorId(userVM.Id);
                if (user == null)
                {
                    return (false, mensaje);
                }

                // Validar IdRol solo si es un administrador y se proporciona un nuevo rol
                if (!userVM.EsEdicionPropia && userVM.IdRol != user.IdRol)
                {
                    if (userVM.IdRol == 0 || !await _context.Roles.AnyAsync(r => r.Id == userVM.IdRol))
                    {
                        return (false, "El rol seleccionado no es válido");
                    }
                }

                await ActualizarUsuario(userVM, user);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                var (user, mensaje) = await ObtenerPorId(userVM.Id);
                if (user == null)
                {
                    return (false, mensaje);
                }
                _context.ReloadEntity(user);
                await ActualizarUsuario(userVM, user);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error inesperado al editar el usuario: {Message}", ex.Message);
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o inténtelo de nuevo más tarde.");
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
        public async Task<(bool, string)> EliminarUsuario(int id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Usuarios.Include(x => x.Pedidos).FirstOrDefaultAsync(m => m.Id == id);
                if (user == null)
                {
                    return (false, "Usuario no encontrado");
                }
                if (user.Pedidos.Any())
                {
                    return (false, "El usuario no se puede eliminar porque tiene pedidos asociados.");
                }
                if (user.HistorialPedidos.Any())
                {
                    return (false, "El usuario no se puede eliminar porque tiene historial de pedidos asociados.");
                }
                await _context.DeleteEntityAsync(user);
                await transaction.CommitAsync();
                return (true, "Usuario eliminado con éxito");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar un usuario con {id}",id);
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
               
            }
           
        } 
        public async Task<(bool, string)> BajaUsuario(int id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var (usuarioDB, mensaje) = await ObtenerPorId(id);
                if (usuarioDB == null)
                {
                    return (false, "El usuario que intenta dar de baja no existe");
                }
                usuarioDB.BajaUsuario = true;

                await   _context.UpdateEntityAsync(usuarioDB);
                await transaction.CommitAsync();
                return (true, "Usuario dado de baja con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al dar de baja el usuario");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }
            
        }
        public async Task<(bool, string)> AltaUsuario(int id)
        {
            using var transaction=await _context.Database.BeginTransactionAsync();
            try
            {
                var (usuarioDB, mensaje) = await ObtenerPorId(id);
                if (usuarioDB == null)
                {
                    return (false, "El usuario que intenta dar de baja no existe");
                }
                usuarioDB.BajaUsuario = false;

                await  _context.UpdateEntityAsync(usuarioDB);
                await transaction.CommitAsync();
                return (true, "Usuario dado de alta con exito");
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error al dar de alta el usuario");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
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
        public async Task<(bool, string)> CrearRol(string nombreRol, List<int> permisoIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                
                if (string.IsNullOrWhiteSpace(nombreRol))
                {
                    _logger.LogWarning("Intento de crear rol con nombre vacío.");
                    return (false, "El nombre del rol no puede estar vacío.");
                }

                if (!Regex.IsMatch(nombreRol, @"^[a-zA-Z0-9]+$"))
                {
                    _logger.LogWarning($"Nombre de rol inválido: {nombreRol}. Solo se permiten letras y números.");
                    return (false, "El nombre del rol solo puede contener letras y números.");
                }

               
                if (await _context.Roles.AnyAsync(r => r.Nombre == nombreRol))
                {
                    _logger.LogWarning($"Intento de crear rol duplicado: {nombreRol}.");
                    return (false, "El nombre del rol ya existe. Proporcione otro nombre.");
                }

               
                if (permisoIds != null && permisoIds.Any())
                {
                    var permisosExistentes = await _context.Permisos
                        .Where(p => permisoIds.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToListAsync();

                    if (permisosExistentes.Count != permisoIds.Count)
                    {
                        var permisosNoEncontrados = permisoIds.Except(permisosExistentes).ToList();
                        _logger.LogWarning($"Permisos no encontrados: {string.Join(", ", permisosNoEncontrados)}.");
                        return (false, $"Los siguientes permisos no existen: {string.Join(", ", permisosNoEncontrados)}.");
                    }
                }

              
                var rol = new Role { Nombre = nombreRol };
                await _context.AddEntityAsync(rol);
             
                _logger.LogInformation($"Rol creado: {nombreRol} (ID: {rol.Id}).");

               
                if (permisoIds != null && permisoIds.Any())
                {
                    foreach (var permisoId in permisoIds)
                    {
                        var rolPermiso = new RolePermiso
                        {
                            RoleId = rol.Id,
                            PermisoId = permisoId
                        };
                      await  _context.AddEntityAsync(rolPermiso);
                    }
                  
                    _logger.LogInformation($"Permisos asignados al rol {nombreRol}: {string.Join(", ", permisoIds)}.");
                }
                await transaction.CommitAsync();
                return (true, "Rol creado y permisos asignados con éxito.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear rol {nombreRol}.");
                await transaction.RollbackAsync();
                return (false, $"Error al crear el rol: {ex.Message}");
            }

        }
        public async Task<(bool, List<int>, string)> CrearPermisos(List<NewPermisoDTO> nuevosPermisos)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var nuevosPermisoIds = new List<int>();

                if (nuevosPermisos == null || !nuevosPermisos.Any())
                {
                    return (true, nuevosPermisoIds, "No se proporcionaron nuevos permisos.");
                }

                foreach (var nuevoPermiso in nuevosPermisos)
                {
                    if (string.IsNullOrWhiteSpace(nuevoPermiso.Nombre))
                    {
                        _logger.LogWarning("Intento de crear permiso con nombre vacío.");
                        return (false, new List<int>(), "El nombre del permiso no puede estar vacío.");

                    }

                    if (await _context.Permisos.AnyAsync(p => p.Nombre == nuevoPermiso.Nombre))
                    {
                        _logger.LogWarning($"Intento de crear permiso duplicado: {nuevoPermiso.Nombre}.");
                        return (false, new List<int>(), $"El permiso '{nuevoPermiso.Nombre}' ya existe.");
                    }

                    var permiso = new Permiso
                    {
                        Nombre = nuevoPermiso.Nombre,
                        Descripcion = nuevoPermiso.Descripcion
                    };
                    await _context.AddEntityAsync(permiso);                
                    nuevosPermisoIds.Add(permiso.Id);

                    _logger.LogInformation($"Permiso creado: {nuevoPermiso.Nombre} (ID: {permiso.Id}).");
                }
                await transaction.CommitAsync();

                return (true, nuevosPermisoIds, "Permisos creados con éxito.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear permisos.");
                await transaction.RollbackAsync();
                return (false, new List<int>(), $"Error al crear permisos: {ex.Message}");
            }
        }
    }
}
