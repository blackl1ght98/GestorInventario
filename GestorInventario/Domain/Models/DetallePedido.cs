using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class DetallePedido
{
    public int Id { get; set; }

    public int? PedidoId { get; set; }

    public int? ProductoId { get; set; }

    public int? Cantidad { get; set; }

    public bool? Rembolsado { get; set; }

    public virtual Pedido? Pedido { get; set; }

    public virtual Producto? Producto { get; set; }
}
