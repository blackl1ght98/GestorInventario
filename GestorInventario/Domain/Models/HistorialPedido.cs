using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class HistorialPedido
{
    public int Id { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Accion { get; set; }

    public string? Ip { get; set; }

    public int? IdUsuario { get; set; }

    public virtual ICollection<DetalleHistorialPedido> DetalleHistorialPedidos { get; set; } = new List<DetalleHistorialPedido>();

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
