using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Usuarios
{
    public class RolesViewModel
    {
        public required List<Role> Roles { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
       
    }
}
