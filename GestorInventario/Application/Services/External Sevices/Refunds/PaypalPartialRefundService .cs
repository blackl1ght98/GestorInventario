using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Application.DTOS.Paypal.Requests.POST;
using GestorInventario.Application.DTOS.Paypal.Responses.Error;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Refund;
using GestorInventario.Application.Services.Common;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.External_Sevices.Refunds
{
    public class PaypalPartialRefundService : PaypalRefundBaseService, IPaypalPartialRefundService
    {
        private readonly IPaypalOrderService _orderService;

        public PaypalPartialRefundService(
            ILogger<PaypalPartialRefundService> logger,
            IPayPalHttpClient paypal,
            IPedidoRepository pedidoRepository,
            IPaypalOrderService orderService) : base(logger, paypal, pedidoRepository)
        {
            _orderService = orderService;
        }

        public async Task<OperationResult<(int pedidoId, int detalleId, string refundId, decimal montoRembolsado, string motivo, string estadoVenta, decimal precioProducto)>> 
            RefundPartialAsync(int pedidoId, string currency, string motivo)
        {
            try
            {
                var pedido = await _pedidoRepository.GetProductoDePedidoAsync(pedidoId);

                // ✅ Calcular el monto REAL a reembolsar (precio + IVA)
                var precioSinIva = pedido.Data.precioProducto;
                var ivaUnitario = CalculadoraFiscal.CalcularIvaUnitario(precioSinIva);
                var montoSolicitadoConIva = precioSinIva + ivaUnitario;

                _logger.LogInformation(
                    "Reembolso parcial pedido {PedidoId} -> Precio:{Precio} IVA:{Iva} Total:{Total}",
                    pedidoId, precioSinIva, ivaUnitario, montoSolicitadoConIva);

                var captureDetails = await _orderService.ObtenerDetallesPagoEjecutadoAsync(pedido.Data.paymentId);
                var (montoReembolso, montoDisponible, estadoVenta) = CalcularMontoDisponibleYEstado(
                    captureDetails, montoSolicitadoConIva, currency);

                // Construir request y ejecutar reembolso en PayPal
                var refundRequest = BuildRefundRequest(montoReembolso, pedido.Data.currency);

                var requestJson = JsonConvert.SerializeObject(refundRequest);
                _logger.LogInformation("Refund request JSON: {RequestJson}", requestJson);

                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v2/payments/captures/{pedido.Data.captureId}/refund",
                    refundRequest,
                    async resp =>
                    {
                        var errBody = await resp.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al procesar reembolso parcial: {resp.StatusCode} - {errBody}");
                    });

                // Consultar estado actualizado (para falsos positivos y estado final)
                var updatedCapture = await _orderService.ObtenerDetallesPagoEjecutadoAsync(pedido.Data.paymentId);

                // Manejo de errores específicos 
                if (!responseBody.StartsWith("{") || responseBody.Contains("error"))
                {
                    var errorDetails = JsonConvert.DeserializeObject <PaypalErrorResponse > (responseBody);
                    string errorMessage = $"Error al procesar el reembolso: {errorDetails?.Message ?? "Error desconocido"}";

                    if (errorDetails?.Details?.Any(d => d.Issue == "REFUND_AMOUNT_EXCEEDED") == true)
                    {
                        //Corregido: usar el monto con IVA en el mensaje
                        errorMessage = $"El monto ({montoSolicitadoConIva} {currency}) excede disponible ({montoDisponible} {currency}).";

                        // Comparar con el monto correcto formateado 
                        var montoFormateado = CalculadoraFiscal.FormatearPayPal(montoSolicitadoConIva);
                        var recentRefund = updatedCapture?.PurchaseUnits[0].Payments.Refunds?
                            .FirstOrDefault(r => r.Amount.Value == montoFormateado);

                        if (recentRefund != null)
                        {
                            _logger.LogWarning("Falso positivo: Reembolso ya procesado (ID {RefundId}).", recentRefund.Id);
                            responseBody = JsonConvert.SerializeObject(new PaypalRefundResponseDto { Id = recentRefund.Id });
                        }
                        else
                        {
                            throw new InvalidOperationException(errorMessage);
                        }
                    }
                    else if (errorDetails?.Details?.Any() == true)
                    {
                        errorMessage += $". Detalles: {string.Join(", ", errorDetails.Details.Select(d => $"{d.Issue}: {d.Description}"))}";
                        throw new InvalidOperationException(errorMessage);
                    }
                }

                _logger.LogInformation("Reembolso parcial exitoso: {ResponseBody}", responseBody);

                var refundResponse = JsonConvert.DeserializeObject <PaypalRefundResponseDto > (responseBody)
                    ?? throw new InvalidOperationException("No se pudo deserializar respuesta de reembolso");

                string refundId = refundResponse.Id;

                return OperationResult<(int, int, string, decimal, string, string, decimal)>.Ok("",
                    (pedido.Data.idPedido,
                     pedido.Data.detalleId,
                     refundId,
                     montoReembolso,
                     motivo,
                     estadoVenta,
                     pedido.Data.precioProducto));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validación fallida en reembolso parcial pedido {PedidoId}", pedidoId);
                throw new InvalidOperationException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en reembolso parcial pedido {PedidoId}", pedidoId);
                throw new InvalidOperationException("No se pudo realizar el reembolso parcial. Intenta de nuevo o contacta soporte.", ex);
            }
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