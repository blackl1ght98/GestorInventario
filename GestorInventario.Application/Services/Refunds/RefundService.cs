
using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Application.Interfaces.Application.Services.Refunds;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.Common;
using GestorInventario.Domain.enums.Paypal;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;


namespace GestorInventario.Application.Services.Refunds
{
    public class RefundService: IRefundService
    {
        private readonly IPedidoRepository _pedidoRepository;
        private readonly ICurrentUserAccessor _currentUserAccesor;
        private readonly IPaypalRepository _paypalRepository;
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPaypalRefundService _paypalRefundService;
        private readonly ILogger<RefundService> _logger;

        public RefundService(IPedidoRepository pedidoRepository, ICurrentUserAccessor currentUserAccesor, IPaypalRepository paypalRepository, 
            IPaypalOrderService paypalOrderService, IPaypalRefundService paypalRefundService, ILogger<RefundService> logger)
        {
            _pedidoRepository = pedidoRepository;
            _currentUserAccesor = currentUserAccesor;
            _paypalRepository = paypalRepository;
            _paypalOrderService = paypalOrderService;
            _paypalRefundService = paypalRefundService;
            _logger = logger;
        }

        // ============================================
        // REEMBOLSO TOTAL
        // ============================================
        // Marca TODAS las líneas del pedido como reembolsadas y registra el importe
        // que PayPal devolvió realmente en esta operación (no el total del pedido).
        public async Task<OperationResult<string>> ProcesarRembolsoTotalAsync(
            int pedidoId, string refundId, decimal montoReembolsado)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);
         
            if (pedido == null)
                return OperationResult<string>.Fail();
         
            pedido.EstadoPedido = EstadoPedido.Rembolsado.ToString();
         
            if (pedido.DetallePedidos != null)
            {
                foreach (var detalle in pedido.DetallePedidos)
                {
                    detalle.Rembolsado = true;
                }
            }
         
            await _pedidoRepository.ActualizarPedidoAsync(pedido);
         
            var usuarioActual = _currentUserAccesor.GetCurrentUserId();
         
            var rembolsoExistente = await _paypalRepository.ObtenRembolsoAsync(pedido.NumeroPedido);
         
            // Un registro ya completado (por ejemplo, un parcial anterior) se conserva
            // como historial: el total se guarda como un registro nuevo.
            if (rembolsoExistente == null || rembolsoExistente.ReembolsoCompletado == true)
            {
                var rembolso = new Rembolso
                {
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                    EmailCliente = pedido.IdUsuarioNavigation?.Email,
                    FechaRembolso = DateTime.UtcNow,
                    MotivoRembolso = "Rembolso solicitado por el usuario",
                    EstadoRembolso = EstadoRembolso.EnRevision.ToString(),
                    ReembolsoCompletado = true,
                    UsuarioId = usuarioActual,
                    PedidoId = pedido.Id,
                    RefundIdPayPal = refundId,
                    MontoRembolsado = montoReembolsado,
                    Currency = pedido.Currency,
                    TipoRembolso = TipoRembolso.Total.ToString(),
                };
         
                await _paypalRepository.AgregarRembolsoAsync(rembolso);
                return OperationResult<string>.Ok("Rembolso procesado con éxito");
            }
         
            // Solicitud pendiente (p. ej. EnRevision) que ahora se aprueba.
            rembolsoExistente.EstadoRembolso = EstadoRembolso.Aprobado.ToString();
            rembolsoExistente.ReembolsoCompletado = true;
            rembolsoExistente.TipoRembolso = TipoRembolso.Total.ToString();
            rembolsoExistente.FechaRembolso = DateTime.UtcNow;
            rembolsoExistente.RefundIdPayPal = refundId;
            rembolsoExistente.MontoRembolsado = montoReembolsado;
         
            await _paypalRepository.ActualizarRembolsoAsync(rembolsoExistente);
            return OperationResult<string>.Ok("Rembolso actualizado con éxito");
        }

        // ============================================
        // REEMBOLSO PARCIAL
        // ============================================
      public async Task<OperationResult<(int pedidoId, int detalleId, decimal precioProducto, string motivo)>> RealizarRembolsoParcial(RefundPartialDto request)
{
    // 1. OBTENER DATOS DEL PEDIDO (tu BD)
    var detallePedido = await _pedidoRepository.ObtenerDetalleParaReembolsoAsync(request.DetalleId);
    if (detallePedido == null)
        return OperationResult<(int, int, decimal, string)>.Fail("No se encontró el detalle del pedido.");

    // 2. CALCULAR MONTO CON IVA
    var precioSinIva = detallePedido.Producto.Precio;
    var montoSolicitadoConIva = CalculadoraFiscal.CalcularPrecioConIva(precioSinIva);

    _logger.LogInformation(
        "Reembolso parcial pedido {PedidoId} -> Precio:{Precio} Total con IVA:{Total}",
        request.DetalleId, precioSinIva, montoSolicitadoConIva);

    // 3. VERIFICAR ESTADO ACTUAL EN PAYPAL
    var resultRefund = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(
            detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId)
        .Bind(details =>
            PaypalRefundCalculator.CalcularMontoDisponibleYEstado(details, montoSolicitadoConIva, request.Currency));

    if (resultRefund.IsFailure)
        return OperationResult<(int, int, decimal, string)>.Fail(resultRefund.Message);

    var finalRefund = resultRefund.Data;

    // 4. EJECUTAR REEMBOLSO EN PAYPAL
    var refundResult = await _paypalRefundService.RefundCaptureAsync(
        captureId: detallePedido.Pedido.PayPalPaymentCaptures.First().CaptureId,
        amount: finalRefund.MontoReembolso,
        currency: detallePedido.Pedido.Currency,
        nota: $"Reembolso parcial pedido #{detallePedido.Pedido.Id} - {request.Motivo}");

    if (!refundResult.Success)
    {
        // 5. MANEJO DE FALSO POSITIVO
        if (refundResult.Message.Contains("REFUND_AMOUNT_EXCEEDED") ||
            refundResult.Message.Contains("UnprocessableEntity"))
        {
            var updatedCaptureResult = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(
                detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId);

            if (updatedCaptureResult.IsFailure)
                return OperationResult<(int, int, decimal, string)>.Fail($"Error al verificar estado en PayPal: {updatedCaptureResult.Message}");

            var capture = updatedCaptureResult.Unwrap();

            // --- CORRECCIÓN: Usamos el servicio de Infrastructure para la verificación técnica ---
            bool yaExiste = await _paypalOrderService.VerificarReembolsoExistenteAsync(capture, montoSolicitadoConIva);

            if (yaExiste)
            {
                _logger.LogWarning("Falso positivo: Reembolso ya procesado en PayPal.");

                // Obtenemos el ID del reembolso sin usar PayPalFormatter aquí
                var refundIdExistente = _paypalOrderService.ObtenerRefundIdPorMonto(capture, montoSolicitadoConIva);

                // Simulamos el OK para que el flujo continúe al registro en tu BD
                refundResult = OperationResult<(string, decimal)>.Ok(
                    (refundIdExistente ?? "UNKNOWN", finalRefund.MontoReembolso), "Reembolso ya existente");
            }
            else
            {
                return OperationResult<(int, int, decimal, string)>.Fail(
                    $"El monto ({montoSolicitadoConIva} {request.Currency}) excede disponible ({finalRefund.MontoDisponible} {request.Currency}).");
            }
        }
        else
        {
            return OperationResult<(int, int, decimal, string)>.Fail(refundResult.Message);
        }
    }

    // 6. REGISTRAR EN TU BASE DE DATOS
    var rembolsoParcial = await RegistrarReembolsoParcialAsync(
        detallePedido.Pedido.Id,
        detallePedido.Id,
        request.Motivo,
        finalRefund.MontoReembolso,
        detallePedido.Pedido.Currency,
        refundResult.Data.RefundId
        );

    if (rembolsoParcial.Success)
    {
        return OperationResult<(int, int, decimal, string)>.Ok(
            (detallePedido.Pedido.Id, detallePedido.Id, detallePedido.Producto.Precio, request.Motivo), "Reembolso parcial realizado");
    }

    return OperationResult<(int, int, decimal, string)>.Fail(rembolsoParcial.Message);
}
        // Registra el reembolso parcial y actualiza el estado del pedido.
        // Si tras este reembolso ya no queda ninguna línea pendiente, el pedido
        // pasa a EstadoPedido.Rembolsado (total); si aún quedan líneas, se queda
        // en EstadoPedido.RembolsoParcial.
        private async Task<OperationResult<string>> RegistrarReembolsoParcialAsync(
            int pedidoId, int detalleId, string motivo, decimal montoRembolsado, string currency, string refundId)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);

            if (pedido == null)
                return OperationResult<string>.Fail();

            var detalleReembolsado = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleId);
            if (detalleReembolsado == null)
                return OperationResult<string>.Fail();
          
            if (detalleReembolsado.Rembolsado)
                return OperationResult<string>.Fail($"El detalle con ID {detalleId} ya ha sido reembolsado.");

            var usuarioActual = _currentUserAccesor.GetCurrentUserId();

            var rembolso = new Rembolso
            {
                PedidoId = pedido.Id,
                NumeroPedido = pedido.NumeroPedido,
                NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                EmailCliente = pedido.IdUsuarioNavigation?.Email,
                FechaRembolso = DateTime.UtcNow,
                MotivoRembolso = motivo,
                EstadoRembolso = EstadoRembolso.Aprobado.ToString(),
                ReembolsoCompletado = true,
                UsuarioId = usuarioActual,
                MontoRembolsado = montoRembolsado,
                Currency = currency,
                RefundIdPayPal = refundId,
                TipoRembolso = TipoRembolso.Parcial.ToString()
            };

            await _paypalRepository.AgregarRembolsoAsync(rembolso);

            // Marcar el detalle correcto como reembolsado
            detalleReembolsado.Rembolsado = true;
            await _pedidoRepository.ActualizarDetallePedidoAsync(detalleReembolsado);

            // Si ya no queda ninguna línea pendiente, el pedido pasa a
            // reembolso TOTAL aunque se haya llegado ahí a base de parciales.
            
            bool todosReembolsados = pedido.DetallePedidos.All(d => d.Rembolsado);

            pedido.EstadoPedido = todosReembolsados
                ? EstadoPedido.Rembolsado.ToString()
                : EstadoPedido.RembolsoParcial.ToString();

            await _pedidoRepository.ActualizarPedidoAsync(pedido);

            _logger.LogInformation(
                "Reembolso registrado para pedido {PedidoId}, detalle {DetalleId}. Estado resultante: {Estado}",
                pedidoId, detalleId, pedido.EstadoPedido);

            return OperationResult<string>.Ok("Rembolso registrado con exito");
        }
    }





}

