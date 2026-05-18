using GestorInventario.Application.DTOs.Paypal.Responses.GET.Order;
using GestorInventario.Application.DTOS.Paypal.Requests.POST;
using GestorInventario.Application.DTOS.Paypal.Responses.Error;
using GestorInventario.Application.DTOS.Paypal.Responses.POST.Refund;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using Newtonsoft.Json;
using System.Globalization;

namespace GestorInventario.Application.Services.External_Sevices
{
    public class PaypalRefundService: IPaypalRefundService
    {
        private readonly ILogger<PaypalRefundService> _logger;
       
        private readonly IPayPalHttpClient _paypal;       
        private readonly IPaypalOrderService _order;
        private readonly IPedidoRepository _pedidoRepository;
        private readonly IPaypalService _paypalService;
        public PaypalRefundService(ILogger<PaypalRefundService> logger, IPaypalOrderService order,
        IPayPalHttpClient paypal,  IPedidoRepository pedidoRepository, IPaypalService paypalService)
        {
            _logger = logger;
            _paypal = paypal;
            _order = order;
            _pedidoRepository = pedidoRepository;
            _paypalService = paypalService;
        }
        #region Realizar reembolso 
        public async Task<string> RefundSaleAsync(int pedidoId, string currency)
        {
            try
            {
                
                // Obtener el pedido y el monto total desde el repositorio
                var pedido = await _pedidoRepository.GetPedidoWithDetailsAsync(pedidoId);

                // Item2-> representa el totalAmount
                //Item1->representa el pedido
                var refundRequest = BuildRefundRequest(pedido.Data.Item2, pedido.Data.Item1);
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                   HttpMethod.Post,
                   $"v2/payments/captures/{pedido.Data.Item1.CaptureId}/refund",
                   refundRequest,
                    async resp =>
                    {
                        var errBody = await resp.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al realizar el rembolso: {resp.StatusCode} - {errBody}");
                    });

                var refundResponse = JsonConvert.DeserializeObject<PaypalRefundResponseDto>(responseBody);
                if (refundResponse == null)
                {
                    throw new ArgumentNullException("No se pudo obtener los destalles de la devolucion");
                }
                string refundId = refundResponse.Id;
                var updatedCapture = await _order.ObtenerDetallesPagoEjecutadoAsync(pedido.Data.Item1.OrderId);
                if (updatedCapture == null)
                {
                    throw new ArgumentNullException("No se pudo obtener los detalles actualizados");
                }
                string estadoVenta = updatedCapture.PurchaseUnits[0].Payments.Captures[0].Status;
                await _paypalService.UpdatePedidoStatusAsync(pedidoId, EstadoPedido.Rembolsado.ToString(), refundId, estadoVenta);
                await _paypalService.EnviarEmailNotificacionRembolso(pedidoId, pedido.Data.Item2, "Rembolso Aprobado");
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el reembolso");
                throw new InvalidOperationException("No se pudo realizar el reembolso", ex);
            }
        }
        private PaypalRefundRequest BuildRefundRequest(decimal totalAmount, Pedido pedido)
        {
            const decimal IVA = 0.21m;
            decimal totalConIva = Math.Round(totalAmount * (1 + IVA), 2);
            return new PaypalRefundRequest
            {
                NotaParaElCliente = "Pedido rembolsado",
                Amount = new AmountRefundRequest
                {
                    Value = totalConIva.ToString("F2", CultureInfo.InvariantCulture),
                    CurrencyCode = pedido.Currency
                }
            };
        }
        public async Task<string> RefundPartialAsync(int pedidoId, string currency, string motivo)
        {
            try
            {
                var pedido = await _pedidoRepository.GetProductoDePedidoAsync(pedidoId);
                var detalle = pedido.Data.Item1.Pedido.DetallePedidos.FirstOrDefault();
                if (detalle == null)
                    throw new ArgumentException($"No se encontró el detalle del pedido con ID {pedidoId}.");

                if (detalle.Rembolsado ?? false)
                    throw new InvalidOperationException("El detalle del pedido ya ha sido reembolsado.");


                var captureDetails = await _order.ObtenerDetallesPagoEjecutadoAsync(pedido.Data.Item1.Pedido.OrderId);
                var (montoReembolso, montoDisponible, estadoVenta) = CalcularMontoDisponibleYEstado(
                captureDetails, pedido.Data.Item2, currency);

                // 4. Construir request y ejecutar reembolso en PayPal
                var refundRequest = BuildRefundPartialRequest(montoReembolso, pedido.Data.Item1);
                var requestJson = JsonConvert.SerializeObject(refundRequest);
                _logger.LogInformation("Refund request JSON: {RequestJson}", requestJson);

                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v2/payments/captures/{pedido.Data.Item1.Pedido.CaptureId}/refund",
                    refundRequest,
                    async resp =>
                    {
                        var errBody = await resp.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al procesar reembolso parcial: {resp.StatusCode} - {errBody}");
                    });

                // Consultar estado actualizado (para falsos positivos y estado final)
                var updatedCapture = await _order.ObtenerDetallesPagoEjecutadoAsync(pedido.Data.Item1.Pedido.OrderId);

                // Manejo de errores específicos 
                if (!responseBody.StartsWith("{") || responseBody.Contains("error"))
                {
                    var errorDetails = JsonConvert.DeserializeObject<PaypalErrorResponse>(responseBody);
                    string errorMessage = $"Error al procesar el reembolso: {errorDetails?.Message ?? "Error desconocido"}";

                    if (errorDetails?.Details?.Any(d => d.Issue == "REFUND_AMOUNT_EXCEEDED") == true)
                    {
                        errorMessage = $"El monto ({pedido.Data.Item2} {currency}) excede disponible ({montoDisponible} {currency}).";

                        var recentRefund = updatedCapture?.PurchaseUnits[0].Payments.Refunds?
                            .FirstOrDefault(r => r.Amount.Value == pedido.Data.Item2.ToString("F2", CultureInfo.InvariantCulture));

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

                var refundResponse = JsonConvert.DeserializeObject<PaypalRefundResponseDto>(responseBody)
                    ?? throw new InvalidOperationException("No se pudo deserializar respuesta de reembolso");

                string refundId = refundResponse.Id;

                var producto = pedido.Data.Item1.Producto.NombreProducto;
               
                await _paypalService.RegistrarReembolsoParcialAsync(
                    pedido.Data.Item1.Pedido.Id,
                    detalle.Id,
                    refundId,
                    montoReembolso,
                    motivo,
                    estadoVenta
                );

                var emailSuccess = await _paypalService.EnviarEmailNotificacionRembolso(
                    pedido.Data.Item1.Pedido.Id,
                    pedido.Data.Item2,
                    motivo
                );

                if (!emailSuccess.Success)
                    _logger.LogWarning("Fallo al enviar email de reembolso: {Message}", emailSuccess.Message);

                return responseBody;
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
        private PaypalRefundRequest BuildRefundPartialRequest(decimal totalAmount, DetallePedido pedido)
        {
            return new PaypalRefundRequest
            {
                NotaParaElCliente = "Pedido rembolsado",
                Amount = new AmountRefundRequest
                {
                    Value = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    CurrencyCode = pedido.Pedido.Currency,
                }
            };
        }

        #endregion
    }
}
