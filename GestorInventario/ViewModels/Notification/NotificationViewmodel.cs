using GestorInventario.Application.DTOS.Notification;
using GestorInventario.Domain.enums.Notification;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;

namespace GestorInventario.ViewModels.Notification
{
    public class NotificationViewmodel
    {
        public required List<NotificationDtoViewmodel> Notificaciones { get; set; }
        public required List<PaginasModel> Paginas { get; set; }
        public int TotalPaginas { get; set; }
        public int PaginaActual { get; set; }

        public bool SinNotificaciones => Notificaciones == null || !Notificaciones.Any();

        // Propiedad calculada: Hay notificaciones, pero el 100% de ellas ya están leídas
        public bool TodasNotificaciones => !SinNotificaciones && Notificaciones.All(n => n.Leida);
        public string Tipo { get; set; } = string.Empty;
        public string BadgeCssClass => Tipo switch
        {
            nameof(TipoNotificacion.Info)    => "bg-info",
            nameof(TipoNotificacion.Warning) => "bg-warning text-dark",
            nameof(TipoNotificacion.Success) => "bg-success",
            nameof(TipoNotificacion.Error)   => "bg-danger text-dark",
            _                                => "bg-secondary"
        };
       
      
     
    }
}
