using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class Pedidos
{
    public int Id { get; set; }

    public string NumeroPedido { get; set; } = null!;

    public DateTime FechaPedido { get; set; }

    public string EstadoPedido { get; set; } = null!;

    public int IdUsuario { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Iva { get; set; }

    public decimal Total { get; set; }

    public string Currency { get; set; } = null!;

    public bool EsCarrito { get; set; }

    public string? TrackingNumber { get; set; }

    public string? UrlTracking { get; set; }

    public string? Transportista { get; set; }

    public virtual ICollection<DetallePedido> DetallePedido { get; set; } = new List<DetallePedido>();

    public virtual Usuarios IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<PayPalPaymentCaptures> PayPalPaymentCaptures { get; set; } = new List<PayPalPaymentCaptures>();

    public virtual ICollection<PayPalPaymentRefunds> PayPalPaymentRefunds { get; set; } = new List<PayPalPaymentRefunds>();

    public virtual ICollection<Rembolso> Rembolso { get; set; } = new List<Rembolso>();
}
