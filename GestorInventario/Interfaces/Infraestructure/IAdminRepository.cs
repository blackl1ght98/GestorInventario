using GestorInventario.Domain.Models;
using GestorInventario.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Query;

namespace GestorInventario.Interfaces.Infraestructure
{
    public interface IAdminRepository
    {
        IQueryable<Usuario> ObtenerUsuarios();
        Task<Usuario> ObtenerPorId(int id);
        IEnumerable<Role> ObtenerRoles();
      
        Task<Usuario> UsuarioConPedido(int id);
      
        Task<(bool, string)> EditarUsuario(UsuarioEditViewModel userVM);
        Task<(bool, string)> EditarRol(int id, int newRole);
        Task<(bool, string)> CrearUsuario(UserViewModel model);
        Task<(bool, string)> EliminarUsuario(int id);


    }
}
