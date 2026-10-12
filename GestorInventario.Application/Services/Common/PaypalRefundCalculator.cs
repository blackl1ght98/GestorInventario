using GestorInventario.Shared.DTOS.Paypal.Responses.GET.Order;
using System.Globalization;
using GestorInventario.Shared.DTOS.Paypal.BD;
using GestorInventario.Shared.Utilities;


namespace GestorInventario.Application.Services.Common
{
    public static class PaypalRefundCalculator
    {
        
        public static OperationResult<RefundCalculadorDto>
        CalcularMontoDisponibleYEstado(
            OrderDetailsResponse captureDetails,
            decimal montoSolicitado,
            string currency
            )
        {
            try
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

                var grossAmount = ParseDecimalSeguro(capture.Amount?.Value, "monto bruto de la captura");
                var refundedAmount = firstUnit.Payments?.Refunds?
                                         .Where(r => r.Amount?.Value != null)
                                         .Sum(r => ParseDecimalSeguro(r.Amount.Value, "monto de reembolso previo"))
                                     ?? 0m; 
                var availableAmount = grossAmount - refundedAmount; 
                if (availableAmount <= 0)
                {
                    return OperationResult<RefundCalculadorDto>.Fail($"No hay fondos disponibles para reembolsar. Bruto: {grossAmount}, Ya reembolsado: {refundedAmount}");
                
                } 
                var finalRefundAmount = Math.Min(montoSolicitado, availableAmount);

                if (finalRefundAmount < montoSolicitado)
                {
                    return OperationResult<RefundCalculadorDto>.Fail($"Monto solicitado ({montoSolicitado}) excede disponible ({availableAmount}). Ajustando a {finalRefundAmount}.");
                }

                var estadoVenta = finalRefundAmount >= availableAmount && refundedAmount == 0
                    ? "REFUNDED"
                    : "PARTIALLY_REFUNDED";
                var finalRefund = new RefundCalculadorDto()
                {
                    MontoDisponible = availableAmount,
                    MontoReembolso = finalRefundAmount,
                    Estado = estadoVenta
                };
                return OperationResult<RefundCalculadorDto>.Ok(finalRefund);
            }
            catch (InvalidOperationException ex)
            {
               
                return OperationResult<RefundCalculadorDto>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                return OperationResult<RefundCalculadorDto>.Fail($"Ocurrió un error inesperado: {ex.Message}");
            }
            
        }

        private static decimal ParseDecimalSeguro(string? value, string campo)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"El campo '{campo}' no contiene un valor válido.");

            if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                throw new InvalidOperationException($"No se pudo parsear el campo '{campo}': {value}");

            return result;
        }
    }
}
