using GestorInventario.PaginacionLogica;

namespace GestorInventario.Domain.Models.ViewModels.user
{
    public class UsuariosPorRolViewModel
    {
        public List<Usuario> Usuarios { get; set; }
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public int RolId { get; set; }
        public List<Role> TodosLosRoles { get; set; }
    }
}
