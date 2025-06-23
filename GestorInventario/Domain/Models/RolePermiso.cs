using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class RolePermiso
{
    public int RoleId { get; set; }

    public int PermisoId { get; set; }

    public string? Nombre { get; set; }

    public virtual Permiso Permiso { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
