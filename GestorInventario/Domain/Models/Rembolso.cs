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

    public bool? ReembolsoCompletado { get; set; }

    public int UsuarioId { get; set; }

    public int PedidoId { get; set; }

    public decimal MontoRembolsado { get; set; }

    public string? Currency { get; set; }

    public string? RefundIdPayPal { get; set; }

    public virtual Pedido Pedido { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
