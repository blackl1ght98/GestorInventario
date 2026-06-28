using GestorInventario.Domain.Models;

using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Usuarios
{
    public class UsuariosViewModel
    {
        public required List<Usuario> Usuarios { get; set; } 
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public required string Buscar { get; set; }
    }
}
