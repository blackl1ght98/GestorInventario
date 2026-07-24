using GestorInventario.Application.Mappers;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Email;
using GestorInventario.Interfaces.Application.Services.Notification;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.Email;

using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;


namespace GestorInventario.Application.Services.Notification
{
    public class RembolsoNotification: IRembolsoNotification
    {
       
        private readonly ILogger<RembolsoNotification> _logger;
        private readonly IPedidoRepository _pedidoRepository;  
        private readonly IEmailService _emailService;
        public RembolsoNotification( ILogger<RembolsoNotification> logger, IPedidoRepository pedido,  
            IEmailService email)
        {
        
            _logger = logger;
            _pedidoRepository = pedido;             
            _emailService = email;
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
                        ItemQuantity = detalle.Cantidad,
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
