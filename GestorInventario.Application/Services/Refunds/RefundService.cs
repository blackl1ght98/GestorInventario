
using GestorInventario.Application.Services.Common;
using GestorInventario.Domain.enums.Paypal;
using GestorInventario.Domain.enums.Pedido;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Interfaces.Application.Services.Refunds;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.DTOS.Rembolso;
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
        // Marca TODAS las líneas del pedido como reembolsadas y registra
        // el reembolso como TipoRembolso.Total por el importe completo del pedido.
        public async Task<OperationResult<string>> ProcesarRembolsoTotalAsync(
            int pedidoId, string refundId)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);

            if (pedido == null)
                return OperationResult<string>.Fail($"Pedido con ID {pedidoId} no encontrado.");

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

            var obtenerRembolso = await _paypalRepository.ObtenRembolsoAsync(pedido.NumeroPedido);

            if (obtenerRembolso == null)
            {
                var rembolso = new Rembolso
                {
                    NumeroPedido = pedido.NumeroPedido,
                    NombreCliente = pedido.IdUsuarioNavigation?.NombreCompleto,
                    EmailCliente = pedido.IdUsuarioNavigation?.Email,
                    FechaRembolso = DateTime.UtcNow,
                    MotivoRembolso = "Rembolso solicitado por el usuario",
                    EstadoRembolso = EstadoRembolso.Aprobado.ToString(),
                    ReembolsoCompletado = true,
                    UsuarioId = usuarioActual,
                    PedidoId = pedido.Id,
                    RefundIdPayPal = refundId,
                    MontoRembolsado = pedido.Total,
                    Currency = pedido.Currency,
                    TipoRembolso = TipoRembolso.Total.ToString(),
                };

                await _paypalRepository.AgregarRembolsoAsync(rembolso);
                return OperationResult<string>.Ok("Rembolso procesado con éxito");
            }
            else
            {
                obtenerRembolso.EstadoRembolso = EstadoRembolso.Aprobado.ToString();
                obtenerRembolso.ReembolsoCompletado = true;
                obtenerRembolso.TipoRembolso = TipoRembolso.Total.ToString();
                obtenerRembolso.FechaRembolso = DateTime.UtcNow;

                await _paypalRepository.ActualizarRembolsoAsync(obtenerRembolso);
                return OperationResult<string>.Ok("Rembolso actualizado con éxito");
            }
        }

        // ============================================
        // REEMBOLSO PARCIAL
        // ============================================
        public async Task<OperationResult<(int pedidoId, int detalleId, decimal precioProducto, string motivo)>> RealizarRembolsoParcial(RefundPartialDto request)
        {
            // 1. OBTENER DATOS DEL PEDIDO (tu BD)
            var detallePedido = await _pedidoRepository.ObtenerDetalleParaReembolsoAsync(request.DetalleId);
            if (detallePedido == null)
                return OperationResult<(int, int,decimal, string)>.Fail("Su pedido no se encuentra");

            // 2. CALCULAR MONTO CON IVA
            var precioSinIva = detallePedido.Producto.Precio;
            var montoSolicitadoConIva = CalculadoraFiscal.CalcularPrecioConIva(precioSinIva);

            _logger.LogInformation(
                "Reembolso parcial pedido {PedidoId} -> Precio:{Precio} Total con IVA:{Total}",
                request.DetalleId, precioSinIva, montoSolicitadoConIva);

            // 3. VERIFICAR ESTADO ACTUAL EN PAYPAL
            var captureDetails = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(
                detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId);

            var (montoReembolso, montoDisponible, estadoVenta) = PaypalRefundCalculator.CalcularMontoDisponibleYEstado(
                captureDetails, montoSolicitadoConIva, request.Currency, _logger);

            // 4. EJECUTAR REEMBOLSO EN PAYPAL
            var refundResult = await _paypalRefundService.RefundCaptureAsync(
                captureId: detallePedido.Pedido.PayPalPaymentCaptures.First().CaptureId,
                amount: montoReembolso,
                currency: detallePedido.Pedido.Currency,
                nota: $"Reembolso parcial pedido #{detallePedido.Pedido.Id} - {request.Motivo}");

            if (!refundResult.Success)
            {
                // 5. MANEJO DE FALSO POSITIVO
                if (refundResult.Message.Contains("REFUND_AMOUNT_EXCEEDED") ||
                    refundResult.Message.Contains("UnprocessableEntity"))
                {
                    var updatedCapture = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(
                        detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId);
                    var montoFormateado = CalculadoraFiscal.FormatearPayPal(montoSolicitadoConIva);

                    var recentRefund = updatedCapture?.PurchaseUnits[0].Payments.Refunds?
                        .FirstOrDefault(r => r.Amount.Value == montoFormateado);

                    if (recentRefund != null)
                    {
                        _logger.LogWarning("Falso positivo: Reembolso ya procesado (ID {RefundId}).", recentRefund.Id);

                        refundResult = OperationResult<(string, decimal)>.Ok(
                            "Reembolso ya existente",
                            (recentRefund.Id, montoReembolso));
                    }
                    else
                    {
                        return OperationResult<(int,int, decimal, string)>.Fail(
                            $"El monto ({montoSolicitadoConIva} {request.Currency}) excede disponible ({montoDisponible} {request.Currency}).");
                    }
                }
                else
                {
                    return OperationResult<(int, int,decimal, string)>.Fail(refundResult.Message);
                }
            }

            // 6. REGISTRAR EN TU BASE DE DATOS
            var rembolsoParcial = await RegistrarReembolsoParcialAsync(
                detallePedido.Pedido.Id,
                detallePedido.Id,
                request.Motivo,
                montoReembolso,
                detallePedido.Pedido.Currency,
                refundResult.Data.RefundId
                );

            if (rembolsoParcial.Success)
            {
                return OperationResult<(int, int,decimal, string)>.Ok(
                    "Rembolso parcial realizado",
                    (detallePedido.Pedido.Id, detallePedido.Id,detallePedido.Producto.Precio, request.Motivo));
            }
            else
            {
                return OperationResult<(int,int, decimal, string)>.Fail(rembolsoParcial.Message);
            }
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
                return OperationResult<string>.Fail($"Pedido con ID {pedidoId} no encontrado.");

            var detalleReembolsado = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleId);
            if (detalleReembolsado == null)
                return OperationResult<string>.Fail($"Detalle con ID {detalleId} no encontrado.");

            if (detalleReembolsado.Rembolsado ?? false)
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
            bool todosReembolsados = pedido.DetallePedidos.All(d => d.Rembolsado ?? false);

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

