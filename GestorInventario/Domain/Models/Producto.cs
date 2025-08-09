using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Producto
{
    public int Id { get; set; }

    public string NombreProducto { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string? Imagen { get; set; }

    public int Cantidad { get; set; }

    public decimal Precio { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public int? IdProveedor { get; set; }

    public string? UpcCode { get; set; }

    public virtual ICollection<DetalleHistorialPedido> DetalleHistorialPedidos { get; set; } = new List<DetalleHistorialPedido>();

    public virtual ICollection<DetallePedido> DetallePedidos { get; set; } = new List<DetallePedido>();

    public virtual Proveedore? IdProveedorNavigation { get; set; }
}
