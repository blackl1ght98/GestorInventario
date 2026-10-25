using GestorInventario.Application.DTOS;
using GestorInventario.Application.DTOS.Paypal;
using GestorInventario.Application.DTOS.Rembolso;
using GestorInventario.Application.Interfaces.Application.Services.Payment;
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
    public class RefundService : IRefundService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICurrentUserAccessor _currentUserAccesor;
        private readonly IPaypalRepository _paypalRepository;
        private readonly IPaypalOrderService _paypalOrderService;
        private readonly IPaypalRefundService _paypalRefundService;
        private readonly ILogger<RefundService> _logger;
        private readonly IPaymentService _paymentService;

        public RefundService(IOrderRepository orderRepository, ICurrentUserAccessor currentUserAccesor,
            IPaypalRepository paypalRepository, IPaymentService paymentService,
            IPaypalOrderService paypalOrderService, IPaypalRefundService paypalRefundService,
            ILogger<RefundService> logger)
        {
            _orderRepository = orderRepository;
            _currentUserAccesor = currentUserAccesor;
            _paypalRepository = paypalRepository;
            _paypalOrderService = paypalOrderService;
            _paypalRefundService = paypalRefundService;
            _logger = logger;
            _paymentService = paymentService;
        }

        // ============================================
        // REEMBOLSO TOTAL
        // ============================================
        // Marca TODAS las líneas del order como reembolsadas y registra el importe
        // que PayPal devolvió realmente en esta operación (no el total del order).
        public async Task<OperationResult<string>> ProcesarRembolsoTotalAsync(
            int pedidoId, string refundId, decimal montoReembolsado)
        {
            var pedido = await _orderRepository.ObtenerPedidoConDetallesAsync(pedidoId);

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

            await _orderRepository.ActualizarPedidoAsync(pedido);

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
        public async Task<OperationResult<(int pedidoId, int detalleId, decimal precioProducto, string motivo)>>
            RealizarRembolsoParcial(RefundPartialDto request)
        {
            // 1. OBTENER DATOS DEL PEDIDO
            var detallePedido = await _orderRepository.ObtenerDetalleParaReembolsoAsync(request.DetalleId);
            if (detallePedido == null)
                return OperationResult<(int, int, decimal, string)>.Fail("No se encontró el detalle del order.");

            var captureInfo = detallePedido.Pedido.PayPalPaymentCaptures?.FirstOrDefault();
            if (captureInfo == null || string.IsNullOrEmpty(captureInfo.PaymentId) ||
                string.IsNullOrEmpty(captureInfo.CaptureId))
                return OperationResult<(int, int, decimal, string)>.Fail(
                    "El order no tiene captura de PayPal asociada.");

            // 2. CALCULAR MONTO
            var precioSinIva = detallePedido.Producto.Precio;
            var montoSolicitadoConIva = CalculadoraFiscal.CalcularPrecioConIva(precioSinIva);

            // 3. VERIFICAR ESTADO EN PAYPAL
            var resultRefund = await _paypalOrderService
                .ObtenerDetallesPagoEjecutadoAsync(captureInfo.PaymentId)
                .Bind(details =>
                    PaypalRefundCalculator.CalcularMontoDisponibleYEstado(
                        details, montoSolicitadoConIva, request.Currency));

            if (resultRefund.IsFailure)
                return OperationResult<(int, int, decimal, string)>.Fail(resultRefund.Message);

            var finalRefund = resultRefund.Data;

       
            var usuarioActual = _currentUserAccesor.GetCurrentUserId();
            var rembolsoPendiente = new Rembolso
            {
                PedidoId = detallePedido.Pedido.Id,
                NumeroPedido = detallePedido.Pedido.NumeroPedido,
                NombreCliente = detallePedido.Pedido.IdUsuarioNavigation?.NombreCompleto,
                EmailCliente = detallePedido.Pedido.IdUsuarioNavigation?.Email,
                FechaRembolso = DateTime.UtcNow,
                MotivoRembolso = request.Motivo,
                EstadoRembolso = EstadoRembolso.EnRevision.ToString(), // Marcamos como pendiente
                ReembolsoCompletado = false,
                UsuarioId = usuarioActual,
                MontoRembolsado = finalRefund.MontoReembolso,
                Currency = detallePedido.Pedido.Currency,
                TipoRembolso = TipoRembolso.Parcial.ToString()
            };

            await _paypalRepository.AgregarRembolsoAsync(rembolsoPendiente);

            var refundResult = await _paypalRefundService.RefundCaptureAsync(
                captureId: captureInfo.CaptureId,
                amount: finalRefund.MontoReembolso,
                currency: detallePedido.Pedido.Currency,
                nota: $"Reembolso parcial order #{detallePedido.Pedido.Id} - {request.Motivo}");

            if (!refundResult.Success)
            {
                // Manejo de falsos positivos (se mantiene tu lógica original)
                if (refundResult.Message.Contains("REFUND_AMOUNT_EXCEEDED") ||
                    refundResult.Message.Contains("UnprocessableEntity"))
                {
                    var updatedCaptureResult =
                        await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(captureInfo.PaymentId);
                    if (updatedCaptureResult.IsFailure)
                        return OperationResult<(int, int, decimal, string)>.Fail(
                            $"Error al verificar estado en PayPal: {updatedCaptureResult.Message}");

                    var orderDetails = updatedCaptureResult.Unwrap();
                    bool yaExiste =
                        await _paypalRefundService.VerificarReembolsoExistenteAsync(orderDetails,
                            montoSolicitadoConIva);

                    if (yaExiste)
                    {
                        var refundIdExistente =
                            _paypalRefundService.ObtenerRefundIdPorMonto(orderDetails, montoSolicitadoConIva);
                        refundResult = OperationResult<(string RefundId, decimal AmountRefunded)>.Ok(
                            (refundIdExistente ?? "UNKNOWN", finalRefund.MontoReembolso), "Reembolso ya existente");
                    }
                    else
                    {
                        return OperationResult<(int, int, decimal, string)>.Fail($"El monto excede disponible.");
                    }
                }
                else
                {
                    // Marcamos el registro preventivo como fallido antes de salir
                    rembolsoPendiente.EstadoRembolso = "Fallido";
                    await _paypalRepository.ActualizarRembolsoAsync(rembolsoPendiente);
                    return OperationResult<(int, int, decimal, string)>.Fail(refundResult.Message);
                }
            }

            // 5. CONFIRMACIÓN FINAL EN BD
            // Ahora que PayPal confirmó, actualizamos el registro y el estado del order.
            var resultadoFinal = await ConfirmarReembolsoParcialAsync(
                rembolsoPendiente.Id,
                detallePedido.Pedido.Id,
                detallePedido.Id,
                refundResult.Data.RefundId);

            if (resultadoFinal.Success)
            {
                return OperationResult<(int, int, decimal, string)>.Ok(
                    (detallePedido.Pedido.Id, detallePedido.Id, detallePedido.Producto.Precio, request.Motivo),
                    "Reembolso parcial realizado");
            }

            return OperationResult<(int, int, decimal, string)>.Fail(resultadoFinal.Message);
        }

        private async Task<OperationResult<string>> ConfirmarReembolsoParcialAsync(
            int rembolsoId, int pedidoId, int detalleId, string refundId)
        {
            var pedido = await _orderRepository.ObtenerPedidoConDetallesAsync(pedidoId);
            if (pedido == null) return OperationResult<string>.Fail();

            var detalleReembolsado = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleId);
            if (detalleReembolsado == null) return OperationResult<string>.Fail();

            // 1. Actualizar el registro de reembolso preventivo
            var rembolso = await _paypalRepository.ObtenRembolsoByIdAsync(rembolsoId); // Asumo que existe este método
            if (rembolso != null)
            {
                rembolso.EstadoRembolso = EstadoRembolso.Aprobado.ToString();
                rembolso.ReembolsoCompletado = true;
                rembolso.RefundIdPayPal = refundId;
                await _paypalRepository.ActualizarRembolsoAsync(rembolso);
            }

            // 2. Marcar el detalle como reembolsado
            detalleReembolsado.Rembolsado = true;
            await _orderRepository.ActualizarDetallePedidoAsync(detalleReembolsado);

            // 3. Actualizar estado del order (Total o Parcial)
            bool todosReembolsados = pedido.DetallePedidos.All(d => d.Rembolsado);
            pedido.EstadoPedido = todosReembolsados
                ? EstadoPedido.Rembolsado.ToString()
                : EstadoPedido.RembolsoParcial.ToString();

            await _orderRepository.ActualizarPedidoAsync(pedido);

            return OperationResult<string>.Ok("Reembolso confirmado");
        }


        public async Task<OperationResult<RembolsoTotal>> EjecutarReembolsoTotalAsync(RefundFullDto request)
        {
            var pedido = await _orderRepository
                .ObtenerPedidoConDetallesAsync(request.PedidoId);

            if (pedido == null)
                return OperationResult<RembolsoTotal>.Fail();

            var reembolsoPrevio = pedido.Rembolsos?
                .FirstOrDefault(r => r.ReembolsoCompletado == true);

            if (reembolsoPrevio != null)
            {
                return OperationResult<RembolsoTotal>.Ok();
            }

            var captureId = pedido.PayPalPaymentCaptures.FirstOrDefault().CaptureId;
            if (string.IsNullOrEmpty(captureId))
                return OperationResult<RembolsoTotal>.Fail();

            // ============================================
            // 2. CALCULAR MONTO A REEMBOLSAR (todas las líneas
            //    aún no reembolsadas, según BD, con su cantidad real)
            // ============================================

            var detallesPendientes = pedido.DetallePedidos
                .Where(d => !(d.Rembolsado))
                .ToList();

            if (!detallesPendientes.Any())
                return OperationResult<RembolsoTotal>.Fail();

            var (_, _, montoSolicitadoConIva) = CalculadoraFiscal.CalcularTotales(
                detallesPendientes.Select(d => (d.Producto.Precio, d.Cantidad))
            );


            // ============================================
            // 3. VERIFICAR CONTRA PAYPAL (fuente de verdad real)
            // ============================================

            var captureDetails = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(
                pedido.PayPalPaymentCaptures.First().PaymentId);
            var details = captureDetails.Unwrap();

            var resultRefund = PaypalRefundCalculator.CalcularMontoDisponibleYEstado(
                details, montoSolicitadoConIva, request.Currency);
            var finalRefund = resultRefund.Unwrap();

            var refundResult = await _paypalRefundService.RefundCaptureAsync(
                captureId: captureId,
                amount: finalRefund.MontoReembolso,
                currency: request.Currency,
                nota: $"Reembolso order #{pedido.NumeroPedido}");

            if (!refundResult.Success)
                return OperationResult<RembolsoTotal>.Fail();

            var procesar = await ProcesarRembolsoTotalAsync(
                pedido.Id,
                refundResult.Data.RefundId,
                refundResult.Data.AmountRefunded);
            if (!procesar.Success)
            {
                return OperationResult<RembolsoTotal>.Fail();
            }

            var amountRefunded = refundResult.Data.AmountRefunded;
            var pedidoId = pedido.Id;
            var dto = new RembolsoTotal
            {
                PedidoId = pedidoId,
                AmountRefunded = amountRefunded,
                DetallesIds = detallesPendientes.Select(d => d.Id).ToList()
            };
            return OperationResult<RembolsoTotal>.Ok(dto);
        }

        public async Task<OperationResult<string>> RefundFormAsync(RefundDto form)
        {
            var obtenerNumeroPedido = await _orderRepository.ObtenerNumeroPedido(form.NumeroPedido);

            if (obtenerNumeroPedido == null)
            {
                _logger.LogInformation("El número de order proporcionado no existe: {NumeroPedido}",
                    form.NumeroPedido);
                return OperationResult<string>.Fail();
            }

            int usuarioActual = _currentUserAccesor.GetCurrentUserId();

            var emailCliente = _currentUserAccesor.GetCurrentUserEmail();

            if (string.IsNullOrEmpty(emailCliente))
            {
                _logger.LogInformation("El email del usuario actual no se encuentra registrado");
            }

            var pedido = await _orderRepository.ObtenerNumeroPedido(form.NumeroPedido);

            if (pedido == null)
            {
                _logger.LogInformation(
                    "El order con el número proporcionado no existe: {NumeroPedido}", form.NumeroPedido);
                return OperationResult<string>.Fail();
            }

            var capture = pedido.PayPalPaymentCaptures?.FirstOrDefault();
            var orderId = capture?.PaymentId;

            if (string.IsNullOrEmpty(orderId))
            {
                _logger.LogWarning(
                    "El order {NumeroPedido} no tiene PaymentId de PayPal", form.NumeroPedido);
                return OperationResult<string>.Fail();
            }

            var detallespago = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(orderId);

            if (!detallespago.Success)
            {
                _logger.LogError(
                    "Error al obtener detalles de PayPal para orderId {OrderId}: {Message}",
                    orderId, detallespago.Message);
                return OperationResult<string>.Fail();
            }

            var detalles = detallespago.Unwrap(); // PayPalOrderDetailsDto

            // Ya no hace falta comprobar PurchaseUnits ni mapear aquí.
            // El DTO limpio ya trae Items, Captures, etc.
            var resultado = await _paymentService.ProcesarRembolso(
                detalles,
                usuarioActual,
                form,
                obtenerNumeroPedido.Id,
                emailCliente);

            if (!resultado.Success)
            {
                _logger.LogError(
                    "Error al procesar reembolso del order {NumeroPedido}: {Message}",
                    form.NumeroPedido, resultado.Message);
                return OperationResult<string>.Fail();
            }

            return OperationResult<string>.Ok();
        }
    }
}