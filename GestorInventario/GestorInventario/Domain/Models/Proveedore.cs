using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Proveedore
{
    public int Id { get; set; }

    public string NombreProveedor { get; set; } = null!;

    public string Contacto { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
