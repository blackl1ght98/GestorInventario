using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class DetalleHistorialPedido
{
    public int Id { get; set; }

    public int? HistorialPedidoId { get; set; }

    public int? ProductoId { get; set; }

    public int? Cantidad { get; set; }

    public virtual HistorialPedido? HistorialPedido { get; set; }

    public virtual Producto? Producto { get; set; }
}
