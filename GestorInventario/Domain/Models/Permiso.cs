using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Permiso
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<RolePermiso> RolePermisos { get; set; } = new List<RolePermiso>();
}
