using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Users
{
    public class UserByRolViewModel
    {
        public required List<Usuario> Usuarios { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int RolId { get; set; }
        public required List<Role> TodosLosRoles { get; set; }
    }
}
