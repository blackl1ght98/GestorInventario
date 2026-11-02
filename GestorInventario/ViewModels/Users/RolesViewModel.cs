using GestorInventario.Application.DTOS.Role;
using GestorInventario.Application.DTOS.User;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Users
{
    public class RolesViewModel
    {
        public required List<RoleCardViewModel> Roles { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
       
    }
}
