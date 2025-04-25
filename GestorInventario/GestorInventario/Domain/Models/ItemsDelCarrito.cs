using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class ItemsDelCarrito
{
    public int Id { get; set; }

    public int? CarritoId { get; set; }

    public int? ProductoId { get; set; }

    public int? Cantidad { get; set; }

    public string NumeroPedido { get; set; } = null!;

    public DateTime FechaPedido { get; set; }

    public string EstadoPedido { get; set; } = null!;

    public virtual Carrito? Carrito { get; set; }

    public virtual Producto? Producto { get; set; }
}
