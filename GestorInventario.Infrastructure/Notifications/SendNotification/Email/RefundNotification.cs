using GestorInventario.Application.DTOS.Email;
using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Application.Interfaces.Application.Services.Email;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Notifications.SendNotification.Email;
using GestorInventario.Domain.Common;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.Notifications.SendNotification.Email
{
    public class RefundNotification: IRefundNotification
    {
       
        private readonly ILogger<RefundNotification> _logger;
        private readonly IOrderRepository _orderRepository;  
        private readonly IEmailService _emailService;
        public RefundNotification( ILogger<RefundNotification> logger, IOrderRepository order,  
            IEmailService email)
        {
        
            _logger = logger;
            _orderRepository = order;             
            _emailService = email;
        }




        public async Task<OperationResult<string>> EnviarEmailNotificacionRembolso(
      int pedidoId, IEnumerable<int> detalleIdsReembolsados, decimal montoReembolsado, string motivo)
        {
            try
            {
                var pedido = await _orderRepository.ObtenerPedidoConDetallesAsync(pedidoId);

                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró el order con ID {PedidoId}", pedidoId);
                    return OperationResult<string>.Fail("Pedido no encontrado");
                }

                var usuarioPedido = pedido.IdUsuarioNavigation?.Email ?? "Email no disponible";
                var nombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto ?? "Cliente";

                // Solo las líneas que realmente se reembolsaron en ESTA operación
                var detallesReembolsados = pedido.DetallePedidos
                    .Where(d => detalleIdsReembolsados.Contains(d.Id))
                    .ToList();

                var productos = detallesReembolsados.Select(d =>
                {
                    var precioUnitario = d.Producto?.Precio ?? 0;
                    var (subtotalSinIva, iva, totalConIva) = CalculadoraFiscal.CalcularCosteProducto(precioUnitario, d.Cantidad);

                    return new PaypalPaymentItemDto
                    {
                        ItemName = d.Producto?.NombreProducto ?? "N/A",
                        ItemQuantity = d.Cantidad,
                        ItemPrice = precioUnitario,
                        ItemCurrency = pedido.Currency,
                        ItemSku = d.Producto?.Descripcion ?? "N/A",
                        PrecioUnitario = precioUnitario,
                        SubtotalSinIva = subtotalSinIva,
                        Iva = iva,
                        TotalConIva = totalConIva,
                    };
                }).ToList();

                var correo = new EmailReembolsoAprobadoDto
                {
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = nombreCliente,
                    EmailCliente = usuarioPedido,
                    FechaRembolso = DateTime.UtcNow,
                    CantidadADevolver = montoReembolsado,
                    MotivoRembolso = motivo,
                    Productos = productos,
                    EsReembolsoTotal = detallesReembolsados.Count == pedido.DetallePedidos.Count
                };

                await _emailService.EnviarNotificacionReembolsoAsync(correo);

                return OperationResult<string>.Ok("Correo de notificación de reembolso enviado correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación de reembolso para el order ID {PedidoId}", pedidoId);
                return OperationResult<string>.Fail("Error al enviar el correo");
            }
        }


    }
}
