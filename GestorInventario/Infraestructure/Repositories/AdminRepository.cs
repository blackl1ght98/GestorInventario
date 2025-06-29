using AutoMapper;
using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
namespace GestorInventario.Infraestructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IEmailService _emailService;
        private readonly HashService _hashService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<AdminRepository> _logger;
        private readonly IMapper _mapper;
        public AdminRepository(GestorInventarioContext context, IEmailService email, HashService hash, IHttpContextAccessor accessor, ILogger<AdminRepository> logger, IMapper mapper )
        {
            _context = context;
            _emailService = email;
            _hashService = hash;
          _contextAccessor = accessor;
            _logger = logger;
            _mapper = mapper;
        }
       //Todos los metodos que hay aquí se llaman en AdminController
        public async Task<IEnumerable<Usuario>> ObtenerUsuarios() => await _context.Usuarios.Include(x=>x.IdRolNavigation).ToListAsync();
        public async Task<Usuario> ObtenerPorId(int id)=>await _context.Usuarios.Include(x=>x.IdRolNavigation).FirstOrDefaultAsync(x=>x.Id==id);
        public  async Task<IEnumerable<Role>> ObtenerRoles()=> await _context.Roles.ToListAsync();
        public async Task<Usuario> UsuarioConPedido(int id)=>await _context.Usuarios.Include(p => p.Pedidos).FirstOrDefaultAsync(m => m.Id == id);
        public async Task<Usuario> ObtenerUsuarioId(int id) => await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        public async Task<List<Role>> ObtenerRolesConUsuarios()=>await _context.Roles.Include(x=>x.Usuarios).ToListAsync();
        //Metodo que implementa la separación de tuplas para manejar 2 valores
        public async Task<(bool, string)> EliminarUsuario(int id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.Usuarios.Include(x => x.Pedidos).Include(x => x.Carritos).FirstOrDefaultAsync(m => m.Id == id);
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
                _logger.LogError("Error al eliminar un usuario");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
               
            }
           
        } 
        public async Task<(bool, string?)> EditarUsuario(UsuarioEditViewModel userVM)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                if (_contextAccessor.HttpContext.User.Identity.IsAuthenticated)
                {
                    if (_contextAccessor.HttpContext.User.IsInRole("Administrador"))
                    {
                        var user = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == userVM.Id);
                        if (user != null)
                        {                                                 
                            await ActualizarUsuario(userVM, user);
                            await transaction.CommitAsync();
                            return (true, null);
                        }
                        else
                        {                           
                            return (false, "Usuario no encontrado");
                        }
                    }
                    else
                    {                       
                        return (false, "El rol que tiene no tiene permitida esta operación");
                    }
                }
                else
                {
                    return (false, "Usuario no logueado");
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                var user = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == userVM.Id);
                if (user == null)
                {
                    
                    return (false, "Usuario no encontrado");
                }
                _context.ReloadEntity(user);                              
                await ActualizarUsuario(userVM, user);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch(Exception ex)
            {
                await transaction.RollbackAsync(); 
                _logger.LogError(ex, "Error inesperado al editar el usuario");
                return (false, "Ocurrió un error inesperado. Por favor, contacte con el administrador o inténtelo de nuevo más tarde.");
            }
        }
        private async Task ActualizarUsuario(UsuarioEditViewModel userVM, Usuario user)
        {
           _mapper.Map(userVM, user);
            if (user.Email != userVM.Email)
            {
                user.ConfirmacionEmail = false;
                user.Email = userVM.Email;
                var (success, error) = await _emailService.SendEmailAsyncRegister(new DTOEmail { ToEmail = userVM.Email });
                if (success)
                {
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", user.Email);

                }
                else {

                    _logger.LogWarning("Error al enviar correo de confirmación: {Error}", error);

                }

            }
            if (user.IdRol != userVM.IdRol)
            {
                user.IdRol = userVM.IdRol;
            }
           
            _context.EntityModified(user);
            await _context.UpdateEntityAsync(user);
        }
        public async Task<(bool, string)> EditarUsuarioActual(EditarUsuarioActual userVM)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existeUsuario = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var user = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId);
                    if (user != null)
                    {                                                                
                        await ActualizarUsuarioActual(userVM, user);
                        await transaction.CommitAsync();
                        return (true, null);
                    }
                 
                    return (false, "El usuario no se ha encontrado o no estas autenticado");
                }
                  
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await  transaction.RollbackAsync();
                _logger.LogError("Ocurrio excepcion de concurrencia",ex);
                await transaction.RollbackAsync();
                var existeUsuario = _contextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                int usuarioId;
                if (int.TryParse(existeUsuario, out usuarioId))
                {
                    var user = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == usuarioId);
                    if (user != null)
                    {                       
                        await ActualizarUsuarioActual(userVM, user);
                        await transaction.CommitAsync();
                        return (true, null);
                    }                                  
                }
              
                return (false, "El usuario no se ha encontrado o no estas autenticado");
            }
            return (false, "Ocurrio un error al editar el usuario");
           
        }
        private async Task ActualizarUsuarioActual(EditarUsuarioActual userVM, Usuario user)
        {
           _mapper.Map(userVM, user);
            if (user != null && user.Email != userVM.Email)
            {
                user.ConfirmacionEmail = false;
                user.Email = userVM.Email;
                await _context.UpdateEntityAsync(user);
                var (success, error) = await _emailService.SendEmailAsyncRegister(new DTOEmail { ToEmail = userVM.Email });
                if (success)
                {
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", user.Email);

                }
                else
                {

                    _logger.LogWarning("Error al enviar correo de confirmación: {Error}", error);

                }
            }
            else
            {
                user!.Email = userVM.Email;
            }
           
            _context.Entry(user).State = EntityState.Modified;
           await _context.UpdateEntityAsync(user);
        }
      
        public async Task<(bool, string)> CrearUsuario(UserViewModel model)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _context.Usuarios.FirstOrDefaultAsync(x => x.Email == model.Email);
                if (existingUser != null)
                {
                    return (false, "El Email ya existe en base de datos");
                }
                var resultadoHash = _hashService.Hash(model.Password);
                var user = new Usuario()
                {
                    Email = model.Email,
                    Password = resultadoHash.Hash,
                    Salt = resultadoHash.Salt,
                    IdRol = model.IdRol,
                    NombreCompleto = model.NombreCompleto,
                    FechaNacimiento = model.FechaNacimiento,
                    Telefono = model.Telefono,
                    Direccion = model.Direccion,
                    FechaRegistro = DateTime.Now,
                    Ciudad=model.ciudad,
                    CodigoPostal=model.codigoPostal
                };
                _context.AddEntity(user);
                var (success, error) = await _emailService.SendEmailAsyncRegister(new DTOEmail { ToEmail = model.Email });
                if (success)
                {
                   
                    _logger.LogInformation("Correo de confirmación enviado a {Email}", user.Email);
                   
                }
                else
                {

                    _logger.LogWarning("Error al enviar correo de confirmación: {Error}", error);
                  
                }
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex,"Error crear el usuario");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }           
        }
     
        public async Task<(bool, string)> BajaUsuario(int id)
        {
            using var transaction= await _context.Database.BeginTransactionAsync();
            try
            {
                var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == id);
                if (usuarioDB == null)
                {
                    return (false, "El usuario que intenta dar de baja no existe");
                }
                usuarioDB.BajaUsuario = true;

                _context.UpdateEntity(usuarioDB);
                await transaction.CommitAsync();
                return (true, null);
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
                var usuarioDB = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == id);
                if (usuarioDB == null)
                {
                    return (false, "El usuario que intenta dar de baja no existe");
                }
                usuarioDB.BajaUsuario = false;

                await  _context.UpdateEntityAsync(usuarioDB);
                await transaction.CommitAsync();
                return (true, null);
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
          
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
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
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }
        public async Task<(bool, string)> CrearRol(string nombreRol, List<int> permisoIds)
        {
            try
            {
                // Validar nombre del rol
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

                // Verificar si el rol ya existe
                if (await _context.Roles.AnyAsync(r => r.Nombre == nombreRol))
                {
                    _logger.LogWarning($"Intento de crear rol duplicado: {nombreRol}.");
                    return (false, "El nombre del rol ya existe. Proporcione otro nombre.");
                }

                // Validar permisos
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

                // Crear el rol
                var rol = new Role { Nombre = nombreRol };
                await _context.Roles.AddAsync(rol);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Rol creado: {nombreRol} (ID: {rol.Id}).");

                // Asignar permisos al rol
                if (permisoIds != null && permisoIds.Any())
                {
                    foreach (var permisoId in permisoIds)
                    {
                        var rolPermiso = new RolePermiso
                        {
                            RoleId = rol.Id,
                            PermisoId = permisoId
                        };
                        _context.RolePermisos.Add(rolPermiso);
                    }
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Permisos asignados al rol {nombreRol}: {string.Join(", ", permisoIds)}.");
                }

                return (true, "Rol creado y permisos asignados con éxito.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al crear rol {nombreRol}.");
                return (false, $"Error al crear el rol: {ex.Message}");
            }

        }
        public async Task<List<Permiso>> ObtenerPermisos() => await _context.Permisos.ToListAsync();
        public async Task<(bool, List<int>, string)> CrearPermisos(List<NewPermisoDTO> nuevosPermisos)
        {
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
                        return (false, null, "El nombre del permiso no puede estar vacío.");
                    }

                    if (await _context.Permisos.AnyAsync(p => p.Nombre == nuevoPermiso.Nombre))
                    {
                        _logger.LogWarning($"Intento de crear permiso duplicado: {nuevoPermiso.Nombre}.");
                        return (false, null, $"El permiso '{nuevoPermiso.Nombre}' ya existe.");
                    }

                    var permiso = new Permiso
                    {
                        Nombre = nuevoPermiso.Nombre,
                        Descripcion = nuevoPermiso.Descripcion
                    };
                    await _context.Permisos.AddAsync(permiso);
                    await _context.SaveChangesAsync();
                    nuevosPermisoIds.Add(permiso.Id);
                    _logger.LogInformation($"Permiso creado: {nuevoPermiso.Nombre} (ID: {permiso.Id}).");
                }

                return (true, nuevosPermisoIds, "Permisos creados con éxito.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear permisos.");
                return (false, null, $"Error al crear permisos: {ex.Message}");
            }
        }
    }
}
