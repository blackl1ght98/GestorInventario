using GestorInventario.Domain.Models;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.ViewModels.user
{
    public class RolesViewModel
    {
        public List<Role> Roles { get; set; }
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public string Buscar { get; set; } // Opcional, para futura funcionalidad de búsqueda
    }
}
