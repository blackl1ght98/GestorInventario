using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Productos
{
    public int Id { get; set; }

    public string NombreProducto { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string Imagen { get; set; } = null!;

    public int Cantidad { get; set; }

    public decimal Precio { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaModificacion { get; set; }

    public int IdProveedor { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public string? TipoCodigoBarras { get; set; }

    public string CodigoBarrasImagen { get; set; } = null!;

    public virtual ICollection<DetallePedido> DetallePedido { get; set; } = new List<DetallePedido>();

    public virtual Proveedores IdProveedorNavigation { get; set; } = null!;
}
