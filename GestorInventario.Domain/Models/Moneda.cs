using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Moneda
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;
}
