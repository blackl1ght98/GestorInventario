using GestorInventario.Application.DTOs.Email;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Repositories;

namespace GestorInventario.Application.Services.Notifications
{
    public class ReembolsoNotificationService : IReembolsoNotificationService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<ReembolsoNotificationService> _logger;

        public ReembolsoNotificationService(
            IPedidoRepository pedidoRepository,
            IEmailService emailService,
            ILogger<ReembolsoNotificationService> logger)
        {
            _pedidoRepository = pedidoRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<OperationResult<string>> EnviarEmailNotificacionRembolso(int pedidoId, decimal montoReembolsado, string motivo)
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

                var productosConDetalles = pedido.DetallePedidos?
                    .Select(detalle => new PayPalPaymentItem
                    {
                        ItemName = detalle.Producto?.NombreProducto ?? "N/A",
                        ItemQuantity = detalle.Cantidad ?? 0,
                        ItemPrice = detalle.Producto?.Precio ?? 0,
                        ItemCurrency = pedido.Currency,
                        ItemSku = detalle.Producto?.Descripcion ?? "N/A"
                    })
                    .ToList() ?? new List<PayPalPaymentItem>();

                var correo = new EmailReembolsoAprobadoDto
                {
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = nombreCliente,
                    EmailCliente = usuarioPedido,
                    FechaRembolso = DateTime.UtcNow,
                    CantidadADevolver = montoReembolsado,
                    MotivoRembolso = motivo,
                    Productos = productosConDetalles
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