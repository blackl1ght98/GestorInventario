namespace GestorInventario.Application.DTOS;

public class FacturaPdfDto
{
    // Encabezado general
    public string IdPago { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string? OrderStatus { get; set; }
    public string? Description { get; set; }

    // Datos del cliente
    public string PayerNombreCompleto { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;
    public string DireccionEnvio { get; set; } = string.Empty;

    // Captura de PayPal
    public string? UltimoCaptureId { get; set; }
    public string? UltimoCaptureStatus { get; set; }
    public decimal TransaccionFeeAmount { get; set; }
    public string? TransaccionFeeCurrency { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal ReceivableAmount { get; set; }
    public string? ReceivableCurrency { get; set; }

    // Totales globales
    public decimal AmountItemTotal { get; set; }
    public decimal AmountShipping { get; set; }
    public decimal AmountTotal { get; set; }
    public string? AmountCurrency { get; set; }

    // Ítems de la factura
    public List<FacturaItemDto> Items { get; set; } = new();
}

public class FacturaItemDto
{
    public string? ItemName { get; set; }
    public int ItemQuantity { get; set; }
    public decimal ItemPrice { get; set; }
    public decimal ItemTax { get; set; }
    public string? ItemCurrency { get; set; }
}