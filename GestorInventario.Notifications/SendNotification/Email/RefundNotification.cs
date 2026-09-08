using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Notifications.EmailServices;
using GestorInventario.Interfaces.Notifications.SendNotification.Email;
using GestorInventario.Shared.DTOS.Email;
using GestorInventario.Shared.DTOS.Rembolso;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;


namespace GestorInventario.Notifications.SendNotification.Email
{
    public class RefundNotification: IRefundNotification
    {
       
        private readonly ILogger<RefundNotification> _logger;
        private readonly IPedidoRepository _pedidoRepository;  
        private readonly IEmailService _emailService;
        public RefundNotification( ILogger<RefundNotification> logger, IPedidoRepository pedido,  
            IEmailService email)
        {
        
            _logger = logger;
            _pedidoRepository = pedido;             
            _emailService = email;
        }




        public async Task<OperationResult<string>> EnviarEmailNotificacionRembolso(
      int pedidoId, IEnumerable<int> detalleIdsReembolsados, decimal montoReembolsado, string motivo)
        {
            try
            {
                var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);

                if (pedido == null)
                {
                    _logger.LogWarning("No se encontró el pedido con ID {PedidoId}", pedidoId);
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
                _logger.LogError(ex, "Error al enviar notificación de reembolso para el pedido ID {PedidoId}", pedidoId);
                return OperationResult<string>.Fail("Error al enviar el correo");
            }
        }


    }
}
