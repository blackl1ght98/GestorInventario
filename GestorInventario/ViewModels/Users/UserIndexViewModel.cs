using GestorInventario.Application.DTOS.User;


using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Users
{
    public class UserIndexViewModel
    {
        public required List<UserDtoViewmodel> Usuarios { get; set; } 
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }
    }
}
