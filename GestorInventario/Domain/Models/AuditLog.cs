using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class AuditLog
{
    public long Id { get; set; }

    public DateTime Fecha { get; set; }

    public int? UsuarioId { get; set; }

    public string Tabla { get; set; } = null!;

    public string Operacion { get; set; } = null!;

    public int RegistroId { get; set; }

    public string? Campo { get; set; }

    public string? ValorAnterior { get; set; }

    public string? ValorNuevo { get; set; }

    public string? IpAddress { get; set; }

    public string? Descripcion { get; set; }
}
