using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class HistorialProducto
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Accion { get; set; }

    public string? Ip { get; set; }

    public virtual ICollection<DetalleHistorialProducto> DetalleHistorialProductos { get; set; } = new List<DetalleHistorialProducto>();

    public virtual Usuario? Usuario { get; set; }
}
