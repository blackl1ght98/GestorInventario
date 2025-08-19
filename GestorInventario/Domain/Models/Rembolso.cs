using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Rembolso
{
    public int Id { get; set; }

    public string? NumeroPedido { get; set; }

    public string? NombreCliente { get; set; }

    public string? EmailCliente { get; set; }

    public DateTime? FechaRembolso { get; set; }

    public string? MotivoRembolso { get; set; }

    public string? EstadoRembolso { get; set; }

    public bool? RembosoRealizado { get; set; }

    public int? UsuarioId { get; set; }

    public string? EstadoVenta { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
