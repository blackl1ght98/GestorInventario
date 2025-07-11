using GestorInventario.Domain.Models;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.ViewModels.user
{
    public class UsuariosViewModel
    {
        public List<Usuario> Usuarios { get; set; } 
        public List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
        public string Buscar { get; set; }
    }
}
