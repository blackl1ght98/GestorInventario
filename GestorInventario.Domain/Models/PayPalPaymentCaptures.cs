using System;
using System.Collections.Generic;

namespace GestorInventario.Domain.Models;

public partial class PayPalPaymentCaptures
{
    public int Id { get; set; }

    public string PaymentId { get; set; } = null!;

    public string CaptureId { get; set; } = null!;

    public int PedidoId { get; set; }

    public string StatusCapture { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = null!;

    public string ProtectionEligibility { get; set; } = null!;

    public decimal TransactionFeeAmount { get; set; }

    public string TransactionFeeCurrency { get; set; } = null!;

    public decimal ReceivableAmount { get; set; }

    public string ReceivableCurrency { get; set; } = null!;

    public decimal ExchangeRate { get; set; }

    public bool FinalCapture { get; set; }

    public string DisputeCategories { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }

    public virtual PayPalPaymentDetails Payment { get; set; } = null!;

    public virtual Pedidos Pedido { get; set; } = null!;
}
