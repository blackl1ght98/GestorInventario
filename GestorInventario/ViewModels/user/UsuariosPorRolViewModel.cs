using GestorInventario.Domain.Models;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.ViewModels.user
{
    public class UsuariosPorRolViewModel
    {
        public required List<Usuario> Usuarios { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int RolId { get; set; }
        public required List<Role> TodosLosRoles { get; set; }
    }
}
