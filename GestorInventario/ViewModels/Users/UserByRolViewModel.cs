using GestorInventario.Application.DTOS.Role;
using GestorInventario.Application.DTOS.User;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Users
{
    public class UserByRolViewModel
    {
        public required List<RoleUserDtoViewmodel> Usuarios { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int RolId { get; set; }
        public required ListRolDto TodosLosRoles { get; set; }
        public string NombreRol => TodosLosRoles?.Roles?.FirstOrDefault(r => r.Id == RolId)?.Nombre ?? "Rol desconocido";
    }
}
