﻿using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Role
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<RolePermiso> RolePermisos { get; set; } = new List<RolePermiso>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
