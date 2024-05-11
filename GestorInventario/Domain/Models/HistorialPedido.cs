using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class HistorialPedido
{
    public int Id { get; set; }

    public string NumeroPedido { get; set; } = null!;

    public DateTime FechaPedido { get; set; }

    public string EstadoPedido { get; set; } = null!;

    public int? IdUsuario { get; set; }

    public virtual ICollection<DetalleHistorialPedido> DetalleHistorialPedidos { get; set; } = new List<DetalleHistorialPedido>();

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
