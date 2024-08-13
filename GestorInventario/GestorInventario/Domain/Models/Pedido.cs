using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Pedido
{
    public int Id { get; set; }

    public string NumeroPedido { get; set; } = null!;

    public DateTime FechaPedido { get; set; }

    public string EstadoPedido { get; set; } = null!;

    public int? IdUsuario { get; set; }

    public string? SaleId { get; set; }

    public virtual ICollection<DetallePedido> DetallePedidos { get; set; } = new List<DetallePedido>();

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
