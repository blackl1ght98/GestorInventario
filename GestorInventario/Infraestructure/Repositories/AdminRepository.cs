using GestorInventario.Application.DTOs;
using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.MetodosExtension;

using GestorInventario.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace GestorInventario.Infraestructure.Repositories
{
    public class AdminRepository : IAdminRepository
    {
       private readonly GestorInventarioContext _context;
        private readonly IEmailService _emailService;
       private readonly HashService _hashService;
       
        public AdminRepository(GestorInventarioContext context, IEmailService email, HashService hash )
        {
            _context = context;
            _emailService = email;
            _hashService = hash;
          
        }
        public IQueryable<Usuario> ObtenerUsuarios()
        {
            var queryable = from p in _context.Usuarios.Include(x => x.IdRolNavigation)
                            select p;
            return queryable;
        }
        public async Task<Usuario> ObtenerPorId(int id)
        {
            //Alternativa --> _context.Usuarios.ExistUserId(confirmar.UserId) esto con metodo de extension
            return await _context.Usuarios.FindAsync(id);
        }
        public IEnumerable<Role> ObtenerRoles()
        {
            return _context.Roles.ToList();
        }
       
        public async Task<Usuario> UsuarioConPedido(int id)
        {
            return await _context.Usuarios.Include(p => p.Pedidos).FirstOrDefaultAsync(m => m.Id == id);

        }



        public async Task<(bool, string)> EliminarUsuario(int id)
        {
            var user = await UsuarioConPedido(id);
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
            _context.DeleteEntity(user);
            return (true, "Usuario eliminado con éxito");
        }

       
    
      
       
        public async Task<(bool,string)> EditarUsuario(UsuarioEditViewModel userVM )
        {
            try
            {
                var user = await _context.Usuarios.FirstOrDefaultAsync(x=>x.Id==userVM.Id);
                if (user != null)
                {
                    // Mapea los datos del ViewModel a la entidad
                    user.NombreCompleto = userVM.NombreCompleto;
                    user.FechaNacimiento = userVM.FechaNacimiento;
                    user.Telefono = userVM.Telefono;
                    user.Direccion = userVM.Direccion;
                    if (user != null && user.Email != userVM.Email)
                    {
                        user.ConfirmacionEmail = false;
                        user.Email = userVM.Email;
                        _context.UpdateEntity(user);
                        await _emailService.SendEmailAsyncRegister(new DTOEmail
                        {
                            ToEmail = userVM.Email
                        });
                    }
                    else
                    {
                        //Si el usuario no cambia el email se queda igual
                        user!.Email = userVM.Email;
                    }
                }
                _context.Entry(user).State = EntityState.Modified;
                _context.UpdateEntity(user);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var user = await _context.Usuarios.FirstOrDefaultAsync(x => x.Id == userVM.Id);
                if (user == null)
                {
                    return (false, "Usuario no encontrado");

                }
                _context.ReloadEntity(user);
                // Mapea los datos del ViewModel a la entidad
                user.NombreCompleto = userVM.NombreCompleto;
                user.FechaNacimiento = userVM.FechaNacimiento;
                user.Telefono = userVM.Telefono;
                user.Direccion = userVM.Direccion;
                if (user != null && user.Email != userVM.Email)
                {
                    user.ConfirmacionEmail = false;
                    user.Email = userVM.Email;
                    _context.UpdateEntity(user);
                    await _emailService.SendEmailAsyncRegister(new DTOEmail
                    {
                        ToEmail = userVM.Email
                    });
                }
                else
                {
                    //Si el usuario no cambia el email se queda igual
                    user!.Email = userVM.Email;
                }

            }

            return (true, null);
        }
        public async Task<(bool, string)> EditarRol(int id, int newRole)
        {
            var usuario= await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return (false, "Usuario no encontrado");
            }
            else
            {
                usuario.IdRol = newRole;
                _context.UpdateEntity(usuario);
            }
            return (true, null);
        }
        public async Task<(bool, string)> CrearUsuario(UserViewModel model)
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
                FechaRegistro = DateTime.Now
            };
            _context.AddEntity(user);
            await _emailService.SendEmailAsyncRegister(new DTOEmail
            {
                ToEmail = model.Email
            });
            return (true, null);
        }
    }
}
