using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentRefunds
{
    public int Id { get; set; }

    public string PaymentId { get; set; } = null!;

    public string RefundId { get; set; } = null!;

    public int? PedidoId { get; set; }

    public string Status { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string? NoteToPayer { get; set; }

    public decimal? TotalRefundedAmount { get; set; }

    public decimal? PaypalFee { get; set; }

    public decimal? NetAmount { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }

    public virtual PayPalPaymentDetails Payment { get; set; } = null!;

    public virtual Pedidos? Pedido { get; set; }
}
