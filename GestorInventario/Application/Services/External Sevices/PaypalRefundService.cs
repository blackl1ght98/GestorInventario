using GestorInventario.Application.DTOs.Response_paypal;
using GestorInventario.Application.DTOs.Response_paypal.GET;
using GestorInventario.Application.DTOs.Response_paypal.POST;
using GestorInventario.Domain.Models;
using GestorInventario.enums;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
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
                var (pedido, totalAmount) = await _pedidoRepository.GetPedidoWithDetailsAsync(pedidoId);

                // Crear el objeto de solicitud de reembolso
                var refundRequest = BuildRefundRequest(totalAmount, pedido);
                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                   HttpMethod.Post,
                   $"v2/payments/captures/{pedido.CaptureId}/refund",
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
                var updatedCapture = await _order.ObtenerDetallesPagoEjecutadoV2(pedido.OrderId);
                if (updatedCapture == null)
                {
                    throw new ArgumentNullException("No se pudo obtener los detalles actualizados");
                }
                string estadoVenta = updatedCapture.PurchaseUnits[0].Payments.Captures[0].Status;
                await _paypalService.UpdatePedidoStatusAsync(pedidoId, EstadoPedido.Rembolsado.ToString(), refundId, estadoVenta);
                await _paypalService.EnviarEmailNotificacionRembolso(pedidoId, totalAmount, "Rembolso Aprobado");
                return responseBody;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al realizar el reembolso");
                throw new InvalidOperationException("No se pudo realizar el reembolso", ex);
            }
        }
        private PaypalRefundResponseDto BuildRefundRequest(decimal totalAmount, Pedido pedido)
        {
            return new PaypalRefundResponseDto
            {
                NotaParaElCliente = "Pedido rembolsado",
                Amount = new AmountRefund
                {
                    Value = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    CurrencyCode = pedido.Currency
                }
            };
        }
        public async Task<string> RefundPartialAsync(int pedidoId, string currency, string motivo)
        {
            try
            {
                var (pedido, totalAmount) = await _pedidoRepository.GetProductoDePedidoAsync(pedidoId);
                var detalle = pedido.Pedido.DetallePedidos.FirstOrDefault();
                if (detalle == null)
                    throw new ArgumentException($"No se encontró el detalle del pedido con ID {pedidoId}.");

                if (detalle.Rembolsado ?? false)
                    throw new InvalidOperationException("El detalle del pedido ya ha sido reembolsado.");


                var captureDetails = await _order.ObtenerDetallesPagoEjecutadoV2(pedido.Pedido.OrderId);
                var (availableAmount, estadoVenta) = await CalcularMontoDisponibleYEstadoAsync(
                    captureDetails, totalAmount, currency);

                // 4. Construir request y ejecutar reembolso en PayPal
                var refundRequest = BuildRefundPartialRequest(availableAmount, pedido);
                var requestJson = JsonConvert.SerializeObject(refundRequest);
                _logger.LogInformation("Refund request JSON: {RequestJson}", requestJson);

                var responseBody = await _paypal.ExecutePayPalRequestAsync<string>(
                    HttpMethod.Post,
                    $"v2/payments/captures/{pedido.Pedido.CaptureId}/refund",
                    refundRequest,
                    async resp =>
                    {
                        var errBody = await resp.Content.ReadAsStringAsync();
                        throw new InvalidOperationException($"Error al procesar reembolso parcial: {resp.StatusCode} - {errBody}");
                    });

                // Consultar estado actualizado (para falsos positivos y estado final)
                var updatedCapture = await _order.ObtenerDetallesPagoEjecutadoV2(pedido.Pedido.OrderId);

                // Manejo de errores específicos 
                if (!responseBody.StartsWith("{") || responseBody.Contains("error"))
                {
                    var errorDetails = JsonConvert.DeserializeObject<PaypalErrorResponse>(responseBody);
                    string errorMessage = $"Error al procesar el reembolso: {errorDetails?.Message ?? "Error desconocido"}";

                    if (errorDetails?.Details?.Any(d => d.Issue == "REFUND_AMOUNT_EXCEEDED") == true)
                    {
                        errorMessage = $"El monto ({totalAmount} {currency}) excede disponible ({availableAmount} {currency}).";

                        var recentRefund = updatedCapture?.PurchaseUnits[0].Payments.Refunds?
                            .FirstOrDefault(r => r.Amount.Value == totalAmount.ToString("F2", CultureInfo.InvariantCulture));

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

                var producto = pedido.Producto.NombreProducto;
               
                await _paypalService.RegistrarReembolsoParcialAsync(
                    pedido.Pedido.Id,
                    detalle.Id,
                    refundId,
                    totalAmount,
                    motivo,
                    estadoVenta
                );

                var emailSuccess = await _paypalService.EnviarEmailNotificacionRembolso(
                    pedido.Pedido.Id,
                    totalAmount,
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
        private async Task<(decimal availableAmount, string estadoVenta)> CalcularMontoDisponibleYEstadoAsync(
        OrderDetailsResponse captureDetails, decimal totalAmount, string currency)
        {
            var capture = captureDetails.PurchaseUnits[0].Payments.Captures[0];

            if (currency != capture.Amount.CurrencyCode)
                throw new InvalidOperationException($"Moneda solicitada ({currency}) no coincide con captura.");

            var netAmount = decimal.Parse(capture.SellerReceivableBreakdown.NetAmount.Value, CultureInfo.InvariantCulture);

            var refundedAmount = captureDetails.PurchaseUnits[0].Payments.Refunds?
                .Sum(r => decimal.Parse(r.SellerPayableBreakdown.NetAmount.Value, CultureInfo.InvariantCulture)) ?? 0m;

            var availableAmount = netAmount - refundedAmount;

            if (totalAmount > availableAmount)
            {
                _logger.LogWarning("Monto solicitado excede disponible. Ajustando.");
                totalAmount = availableAmount;
            }

            if (availableAmount <= 0)
                throw new InvalidOperationException("No hay monto disponible para reembolsar.");

            var estadoVenta = capture.Status ?? "PARTIALLY_REFUNDED";

            return (availableAmount, estadoVenta);
        }
        private PaypalRefundResponseDto BuildRefundPartialRequest(decimal totalAmount, DetallePedido pedido)
        {
            return new PaypalRefundResponseDto
            {
                NotaParaElCliente = "Pedido rembolsado",
                Amount = new AmountRefund
                {
                    Value = totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    CurrencyCode = pedido.Pedido.Currency,
                }
            };
        }

        #endregion
    }
}
