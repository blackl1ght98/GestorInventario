using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Proveedores
{
    public int Id { get; set; }

    public string NombreProveedor { get; set; } = null!;

    public string Contacto { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public int IdUsuario { get; set; }

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<Productos> Productos { get; set; } = new List<Productos>();
}
