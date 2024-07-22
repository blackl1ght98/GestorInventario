using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Carrito
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public virtual ICollection<ItemsDelCarrito> ItemsDelCarritos { get; set; } = new List<ItemsDelCarrito>();

    public virtual Usuario? Usuario { get; set; }
}
