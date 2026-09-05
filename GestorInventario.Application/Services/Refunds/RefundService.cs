using Azure.Core;
using GestorInventario.Application.Services.Common;
using GestorInventario.Application.Services.Orders;
using GestorInventario.Domain.enums.Paypal;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Interfaces.Application.Services.Refunds;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Web;
using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using GestorInventario.Shared.DTOS.Rembolso;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

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

        public async Task<OperationResult<string>> ProcesarRembolsoAsync(
          int pedidoId, string status, string refundId)
        {
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);

            if (pedido == null)
                return OperationResult<string>.Fail($"Pedido con ID {pedidoId} no encontrado.");

            pedido.EstadoPedido = status;

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
       

        public async Task<OperationResult<(int pedidoId,decimal precioProducto,string motivo)>> RealizarRembolsoParcial(RefundPartialDto request)
        {


            // ============================================
            // 1. OBTENER DATOS DEL PEDIDO (tu BD)
            // ============================================
            var detallePedido = await _pedidoRepository.ObtenerDetalleParaReembolsoAsync(request.DetalleId);
            if (detallePedido == null)
                return OperationResult<(int,decimal,string)>.Fail("Su pedido no se encuentra");

            // ============================================
            // 2. CALCULAR MONTO CON IVA 
            // ============================================
            var precioSinIva = detallePedido.Producto.Precio;
            var ivaUnitario = CalculadoraFiscal.CalcularIvaUnitario(precioSinIva);
            var montoSolicitadoConIva = precioSinIva + ivaUnitario;

            _logger.LogInformation(
                "Reembolso parcial pedido {PedidoId} -> Precio:{Precio} IVA:{Iva} Total:{Total}",
                request.DetalleId, precioSinIva, ivaUnitario, montoSolicitadoConIva);

            // ============================================
            // 3. VERIFICAR ESTADO ACTUAL EN PAYPAL
            // ============================================
            var captureDetails = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId);
            var (montoReembolso, montoDisponible, estadoVenta) = CalcularMontoDisponibleYEstado(
                captureDetails, montoSolicitadoConIva, request.Currency);

            // ============================================
            // 4. EJECUTAR REEMBOLSO EN PAYPAL 
            // ============================================
            var refundResult = await _paypalRefundService.RefundCaptureAsync(
                captureId: detallePedido.Pedido.PayPalPaymentCaptures.First().CaptureId,
                amount: montoReembolso,
                currency: detallePedido.Pedido.Currency,
                nota: $"Reembolso parcial pedido #{detallePedido.Pedido.Id} - {request.Motivo}");

            if (!refundResult.Success)
            {
                // ============================================
                // 5. MANEJO DE FALSO POSITIVO 
                // ============================================
                if (refundResult.Message.Contains("REFUND_AMOUNT_EXCEEDED") ||
                    refundResult.Message.Contains("UnprocessableEntity"))
                {
                    var updatedCapture = await _paypalOrderService.ObtenerDetallesPagoEjecutadoAsync(detallePedido.Pedido.PayPalPaymentCaptures.First().PaymentId);
                    var montoFormateado = CalculadoraFiscal.FormatearPayPal(montoSolicitadoConIva);

                    var recentRefund = updatedCapture?.PurchaseUnits[0].Payments.Refunds?
                        .FirstOrDefault(r => r.Amount.Value == montoFormateado);

                    if (recentRefund != null)
                    {
                        _logger.LogWarning("Falso positivo: Reembolso ya procesado (ID {RefundId}).", recentRefund.Id);

                        // Usar el refundId existente como si hubiera funcionado
                        refundResult = OperationResult<(string, decimal)>.Ok(
                            "Reembolso ya existente",
                            (recentRefund.Id, montoReembolso));
                    }
                    else
                    {
                          return OperationResult<(int, decimal, string)>.Fail($"El monto ({montoSolicitadoConIva} {request.Currency}) excede disponible ({montoDisponible} {request.Currency}).");
                    }
                }
                else
                {
                    return OperationResult<(int, decimal, string)>.Fail(refundResult.Message);
                }
            }

            // ============================================
            // 6. REGISTRAR EN TU BASE DE DATOS 
            // ============================================
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
                return OperationResult<(int, decimal, string)>.Ok("Rembolso parcial realizado", (detallePedido.Pedido.Id, detallePedido.Producto.Precio, request.Motivo));
              
            }
            else
            {
                return OperationResult<(int, decimal, string)>.Fail( rembolsoParcial.Message );
            }
        }

        private async Task<OperationResult<string>> RegistrarReembolsoParcialAsync(int pedidoId, int detalleId, string motivo, decimal montoRembolsado, string currency, string refundId)
        {

            // Obtener el pedido con los datos relacionados
            var pedido = await _pedidoRepository.ObtenerPedidoConDetallesAsync(pedidoId);

            if (pedido == null)
                return OperationResult<string>.Fail($"Pedido con ID {pedidoId} no encontrado.");

            // Obtener el detalle específico por ID
            var detalleReembolsado = pedido.DetallePedidos.FirstOrDefault(d => d.Id == detalleId);
            if (detalleReembolsado == null)
                return OperationResult<string>.Fail($"Detalle con ID {detalleId} no encontrado.");

            // Evitar reembolsos duplicados
            if (detalleReembolsado.Rembolsado ?? false)
                return OperationResult<string>.Fail($"El detalle con ID {detalleId} ya ha sido reembolsado.");

            var usuarioActual = _currentUserAccesor.GetCurrentUserId();

            // Crear registro de reembolso
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

            _logger.LogInformation($"Reembolso registrado para pedido {pedidoId}, detalle {detalleId}.");
            return OperationResult<string>.Ok("Rembolso registrado con exito");
        }


        private (decimal montoReembolso, decimal montoDisponible, string estadoVenta)
      CalcularMontoDisponibleYEstado(
          OrderDetailsResponse captureDetails,
          decimal montoSolicitado,
          string currency)
        {
            var firstUnit = captureDetails.PurchaseUnits?.FirstOrDefault()
                ?? throw new InvalidOperationException("La orden no contiene unidades de compra.");

            var capture = firstUnit.Payments?.Captures?.FirstOrDefault()
                ?? throw new InvalidOperationException("La orden no contiene capturas de pago.");

            if (currency != capture.Amount?.CurrencyCode)
            {
                throw new InvalidOperationException(
                    $"Moneda solicitada ({currency}) no coincide con la captura ({capture.Amount?.CurrencyCode}).");
            }

            // Parseo seguro del net amount
            var netAmount = ParseDecimalSeguro(
                capture.SellerReceivableBreakdown?.NetAmount?.Value,
                "monto neto de la captura");

            // Suma de reembolsos previos
            var refundedAmount = firstUnit.Payments?.Refunds?
                .Where(r => r.SellerPayableBreakdown?.NetAmount?.Value != null)
                .Sum(r => ParseDecimalSeguro(r.SellerPayableBreakdown.NetAmount.Value, "monto de reembolso previo"))
                ?? 0m;

            var availableAmount = netAmount - refundedAmount;

            if (availableAmount <= 0)
            {
                _logger.LogWarning("No hay fondos disponibles para reembolsar. Net: {Net}, Ya reembolsado: {Refunded}",
                    netAmount, refundedAmount);
                throw new InvalidOperationException("No hay monto disponible para reembolsar.");
            }

            // Ajustar monto solicitado al disponible
            var finalRefundAmount = Math.Min(montoSolicitado, availableAmount);

            if (finalRefundAmount < montoSolicitado)
            {
                _logger.LogWarning(
                    "Monto solicitado ({Solicitado}) excede disponible ({Disponible}). Ajustando a {Ajustado}.",
                    montoSolicitado, availableAmount, finalRefundAmount);
            }

            // Estado: si reembolsamos todo lo disponible, es refund completo. Si no, parcial.
            var estadoVenta = finalRefundAmount >= availableAmount && refundedAmount == 0
                ? "REFUNDED"
                : "PARTIALLY_REFUNDED";

            return (finalRefundAmount, availableAmount, estadoVenta);
        }
        private static decimal ParseDecimalSeguro(string? value, string campo)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"El campo '{campo}' no contiene un valor válido.");
            }

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                throw new InvalidOperationException($"No se pudo parsear el campo '{campo}': {value}");
            }

            return result;
        }
    }
}
