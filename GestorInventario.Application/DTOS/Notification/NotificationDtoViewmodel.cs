using GestorInventario.Domain.enums.Notification;

namespace GestorInventario.Application.DTOS.Notification;

public class NotificationDtoViewmodel
{
    public string Tipo { get; set; } = string.Empty;
    public bool Leida { get; set; }
    public string Titulo { get; set; }
    public string Mensaje { get; set; }
    public DateTime FechaCreacion { get; set; }
    public int Id { get; set; }
    public string BadgeCssClass => Tipo switch
    {
        nameof(TipoNotificacion.Info)    => "bg-info",
        nameof(TipoNotificacion.Warning) => "bg-warning text-dark",
        nameof(TipoNotificacion.Success) => "bg-success",
        nameof(TipoNotificacion.Error)   => "bg-danger text-dark",
        _                                => "bg-secondary"
    };
}