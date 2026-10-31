using GestorInventario.Domain.Models;
using EstadoPedidoEnum = GestorInventario.Domain.enums.Pedido.EstadoPedido;

namespace GestorInventario.Application.DTOS.Pedidos;

public class PedidoDtoViewModel
{
    public int Id { get; set; }
    public string NumeroPedido { get; set; } = string.Empty;
    public string EstadoPedido { get; set; } = string.Empty;
    public DateTime FechaPedido { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string? Transportista { get; set; }
    public List<DetallePedido> DetallePedidos { get; set; } = new();
    public List<PayPalPaymentCapture> PalPaymentCaptures { get; set; } = new();
    public bool EsCarrito { get; set; }
    public string Currency { get; set; } = string.Empty;
    public required List<string> Carrier { get; set; } 

    //PROPIEDADES CALCULADAS
    public bool EstadoNoCancelable => 
        EstadoPedido == EstadoPedidoEnum.Cancelado.ToString() ||
        EstadoPedido == EstadoPedidoEnum.Entregado.ToString() ||
        EstadoPedido == EstadoPedidoEnum.Enviado.ToString() ||
        EstadoPedido == EstadoPedidoEnum.Rembolsado.ToString() ||
        EstadoPedido == EstadoPedidoEnum.Pagado.ToString();

    public bool EsCancelado => EstadoPedido == EstadoPedidoEnum.Cancelado.ToString();
    public bool EsEnviado => EstadoPedido == EstadoPedidoEnum.Enviado.ToString();
    public bool EsRembolsado => EstadoPedido == EstadoPedidoEnum.Rembolsado.ToString();

    public bool BloquearEntrega => 
        EstadoPedido == EstadoPedidoEnum.Entregado.ToString() || 
        EsCancelado || 
        EsCarrito;

    public bool EsCanceladoOCarrito => EsCancelado || EsCarrito;

    public string BadgeColor => EstadoPedido switch
    {
        var e when e == nameof(EstadoPedidoEnum.Rembolsado) => "warning",
        var e when e == nameof(EstadoPedidoEnum.Entregado) => "success",
        _ => "primary"
    };
}