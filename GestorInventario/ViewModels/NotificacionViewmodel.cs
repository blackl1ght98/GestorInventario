using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels
{
    public class NotificacionViewmodel
    {
        public required List<Notificacion> Notificaciones { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }
     
    }
}
