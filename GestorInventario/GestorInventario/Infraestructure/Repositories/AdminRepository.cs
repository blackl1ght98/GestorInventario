﻿using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Domain.Models.ViewModels;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;
using GestorInventario.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace GestorInventario.Infraestructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly GestorInventarioContext _context;
        private readonly IEmailService _emailService;
        private readonly HashService _hashService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<AdminRepository> _logger;
        public AdminRepository(GestorInventarioContext context, IEmailService email, HashService hash, IHttpContextAccessor accessor, ILogger<AdminRepository> logger )
        {
            _context = context;
            _emailService = email;
            _hashService = hash;
          _contextAccessor = accessor;
            _logger = logger;
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
            user.NombreCompleto = userVM.NombreCompleto;
            if (user.FechaNacimiento != userVM.FechaNacimiento)
            {
                user.FechaNacimiento = userVM.FechaNacimiento;
            }
            user.Telefono = userVM.Telefono;
            user.Direccion = userVM.Direccion;
            if (user.Email != userVM.Email)
            {
                user.ConfirmacionEmail = false;
                user.Email = userVM.Email;
                await _emailService.SendEmailAsyncRegister(new DTOEmail { ToEmail = userVM.Email });
            }
            if (user.IdRol != userVM.IdRol)
            {
                user.IdRol = userVM.IdRol;
            }
            user.Ciudad = userVM.Ciudad;
            user.CodigoPostal = userVM.codigoPostal;
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
            user.NombreCompleto = userVM.NombreCompleto;
            if (user.FechaNacimiento != userVM.FechaNacimiento)
            {
                user.FechaNacimiento = userVM.FechaNacimiento;
            }
            user.Telefono = userVM.Telefono;
            user.Direccion = userVM.Direccion;
            if (user != null && user.Email != userVM.Email)
            {
                user.ConfirmacionEmail = false;
                user.Email = userVM.Email;
                await _context.UpdateEntityAsync(user);
                await _emailService.SendEmailAsyncRegister(new DTOEmail
                {
                    ToEmail = userVM.Email
                });
            }
            else
            {
                user!.Email = userVM.Email;
            }
            user.Ciudad=userVM.ciudad;
            user.CodigoPostal = userVM.codigoPostal;
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
                await _emailService.SendEmailAsyncRegister(new DTOEmail
                {
                    ToEmail = model.Email
                });
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
        
        public async Task<(bool, string)> CrearRol(RolViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var comprobarRol =  await _context.Roles.FirstOrDefaultAsync(x=>x.Nombre==model.nombre);
                if (comprobarRol.Nombre!=null)
                {
                    return (false, "Ya existe un rol con el mismo nombre");
                }
                var rol = new Role()
                {
                    Nombre = model.nombre
                };
                _context.AddEntity(rol);
                await transaction.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crear el rol");
                await transaction.RollbackAsync();
                return (false, "Ocurrio un error inesperado por favor contacte con el administrador o intentelo de nuevo mas tarde");
            }
        }
        public async Task ActualizarRolUsuario(int usuarioId, int rolId)
        {
            // Buscar el usuario en la base de datos
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
            {
                throw new Exception($"Usuario con Id {usuarioId} no encontrado.");
            }

            // Verificar que el rol existe
            var rol = await _context.Roles.FindAsync(rolId);
            if (rol == null)
            {
                throw new Exception($"Rol con Id {rolId} no encontrado.");
            }

            // Actualizar el rol del usuario
            usuario.IdRol = rolId; // O el nombre de la columna que uses para el rol (puede ser IdRol en tu caso)
            _context.Usuarios.Update(usuario);
            await _context.SaveChangesAsync();
        }
    }
}
